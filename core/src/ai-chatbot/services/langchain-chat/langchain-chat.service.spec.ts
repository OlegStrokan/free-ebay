import { TestingModule } from '@nestjs/testing';
import { Logger } from '@nestjs/common';
import { ChatOpenAI } from '@langchain/openai';
import { LangchainChatService, VERCEL_ROLES } from './langchain-chat.service';
import { UserMessageDto } from '../../interfaces/dtos/user-message.dto';
import { ContextAwareMessagesDto } from '../../interfaces/dtos/context-aware-message.dto';
import { AiAgentServerException } from 'src/product/core/product/exceptions/ai-agent-server.exception';
import { AI_TEMPLATES } from '../../utils/constants/templates.constants';
import { HumanMessage, AIMessage } from '@langchain/core/messages';
import { createTestingModule } from 'src/shared/testing/test.module';

describe('LangchainChatService', () => {
  let service: LangchainChatService;
  let mockLlmModel: jest.Mocked<ChatOpenAI>;
  let mockLogger: jest.Mocked<Logger>;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();
    service = module.get<LangchainChatService>(LangchainChatService);
    mockLlmModel = module.get(ChatOpenAI);
    mockLogger = module.get(Logger);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('basicChat', () => {
    const mockUserMessageDto: UserMessageDto = {
      query: 'What is TypeScript?',
    };

    it('should successfully process basic chat request', async () => {
      const mockResponse = new Uint8Array([72, 101, 108, 108, 111]);
      const mockChain = {
        invoke: jest.fn().mockResolvedValue(mockResponse),
      };
      const loadSingleChainSpy = jest
        .spyOn(service as any, 'loadSingleChain')
        .mockReturnValue(mockChain);
      const result = await service.basicChat(mockUserMessageDto);
      expect(loadSingleChainSpy).toHaveBeenCalledWith(
        AI_TEMPLATES.BASIC_CHAT_TEMPLATE,
      );
      expect(mockChain.invoke).toHaveBeenCalledWith({
        input: mockUserMessageDto.query,
      });
      expect(result).toBe('Hello');
    });

    it('should throw AiAgentServerException when chain.invoke fails', async () => {
      const mockError = new Error('Network error');
      const mockChain = {
        invoke: jest.fn().mockRejectedValue(mockError),
      };
      jest.spyOn(service as any, 'loadSingleChain').mockReturnValue(mockChain);
      await expect(service.basicChat(mockUserMessageDto)).rejects.toThrow(
        AiAgentServerException,
      );
    });

    it('should throw AiAgentServerException when non-Error exception occurs', async () => {
      const mockChain = {
        invoke: jest.fn().mockRejectedValue('String error'),
      };
      jest.spyOn(service as any, 'loadSingleChain').mockReturnValue(mockChain);
      await expect(service.basicChat(mockUserMessageDto)).rejects.toThrow(
        AiAgentServerException,
      );
    });
  });

  describe.skip('contextAwareChat', () => {
    const mockContextAwareMessagesDto: ContextAwareMessagesDto = {
      messages: [
        { role: 'user', content: 'Hello' },
        { role: 'assistant', content: 'Hi there!' },
        { role: 'user', content: 'How are you?' },
      ],
    };

    it('should successfully process context aware chat request', async () => {
      const mockResponse = new Uint8Array([
        73, 39, 109, 32, 103, 111, 111, 100,
      ]);
      const mockChain = {
        invoke: jest.fn().mockResolvedValue(mockResponse),
      };
      jest.spyOn(service as any, 'loadSingleChain').mockReturnValue(mockChain);
      const result = await service.contextAwareChat(
        mockContextAwareMessagesDto,
      );
      expect(mockChain.invoke).toHaveBeenCalledWith({
        chat_history: 'user: Hello\nassistant: Hi there!',
        input: 'How are you?',
      });
      expect(result).toBe("I'm good");
    });

    it('should handle empty messages array', async () => {
      const emptyMessagesDto: ContextAwareMessagesDto = {
        messages: [],
      };
      const mockResponse = new Uint8Array([79, 75]);
      const mockChain = {
        invoke: jest.fn().mockResolvedValue(mockResponse),
      };
      jest.spyOn(service as any, 'loadSingleChain').mockReturnValue(mockChain);
      const result = await service.contextAwareChat(emptyMessagesDto);
      expect(mockChain.invoke).toHaveBeenCalledWith({
        chat_history: '',
        input: undefined,
      });
      expect(result).toBe('OK');
    });

    it('should handle single message', async () => {
      const singleMessageDto: ContextAwareMessagesDto = {
        messages: [{ role: 'user', content: 'Hello' }],
      };
      const mockResponse = new Uint8Array([72, 105]);
      const mockChain = {
        invoke: jest.fn().mockResolvedValue(mockResponse),
      };
      jest.spyOn(service as any, 'loadSingleChain').mockReturnValue(mockChain);
      const result = await service.contextAwareChat(singleMessageDto);
      expect(mockChain.invoke).toHaveBeenCalledWith({
        chat_history: '',
        input: 'Hello',
      });
      expect(result).toBe('Hi');
    });

    it('should throw AiAgentServerException when chain.invoke fails', async () => {
      const mockError = new Error('API error');
      const mockChain = {
        invoke: jest.fn().mockRejectedValue(mockError),
      };
      jest.spyOn(service as any, 'loadSingleChain').mockReturnValue(mockChain);
      await expect(
        service.contextAwareChat(mockContextAwareMessagesDto),
      ).rejects.toThrow(AiAgentServerException);
      expect(mockLogger.error).toHaveBeenCalledWith(
        `[contextAwareChat] Failed. Messages: ${JSON.stringify(
          mockContextAwareMessagesDto.messages,
        )}. Error: ${mockError.message}`,
        mockError.stack,
        'LangchainChatService',
      );
    });
  });

  describe.skip('agentChat', () => {
    const mockContextAwareMessagesDto: ContextAwareMessagesDto = {
      messages: [
        { role: 'user', content: 'What is the weather?' },
        { role: 'assistant', content: 'I cannot check the weather.' },
        { role: 'user', content: 'Can you help me with programming?' },
      ],
    };

    it('should successfully process agent chat request without custom prompt', async () => {
      const mockResponse = {
        content: 'I can help you with programming questions!',
      };
      mockLlmModel.invoke = jest.fn().mockResolvedValue(mockResponse);
      const result = await service.agentChat(mockContextAwareMessagesDto);
      expect(mockLlmModel.invoke).toHaveBeenCalledWith({
        input: 'Can you help me with programming?',
        chat_history: [
          new HumanMessage({
            content: 'What is the weather?',
            additional_kwargs: {},
          }),
          new AIMessage({
            content: 'I cannot check the weather.',
            additional_kwargs: {},
          }),
        ],
        agent_scratchpad: [],
      });
      expect(result).toBe('I can help you with programming questions!');
    });

    it('should successfully process agent chat request with custom prompt', async () => {
      const customPrompt = 'You are a helpful programming assistant.';
      const mockResponse = {
        content: 'I am a programming assistant and can help you!',
      };
      mockLlmModel.invoke = jest.fn().mockResolvedValue(mockResponse);
      const result = await service.agentChat(
        mockContextAwareMessagesDto,
        customPrompt,
      );
      expect(mockLlmModel.invoke).toHaveBeenCalledWith({
        input: 'Can you help me with programming?',
        chat_history: [
          new HumanMessage({
            content: 'What is the weather?',
            additional_kwargs: {},
          }),
          new AIMessage({
            content: 'I cannot check the weather.',
            additional_kwargs: {},
          }),
        ],
        agent_scratchpad: [],
      });
      expect(result).toBe('I am a programming assistant and can help you!');
    });

    it('should handle empty messages array', async () => {
      const emptyMessagesDto: ContextAwareMessagesDto = {
        messages: [],
      };
      const mockResponse = {
        content: 'Hello!',
      };
      mockLlmModel.invoke = jest.fn().mockResolvedValue(mockResponse);
      const result = await service.agentChat(emptyMessagesDto);
      expect(mockLlmModel.invoke).toHaveBeenCalledWith({
        input: undefined,
        chat_history: [],
        agent_scratchpad: [],
      });
      expect(result).toBe('Hello!');
    });

    it('should handle single message', async () => {
      const singleMessageDto: ContextAwareMessagesDto = {
        messages: [{ role: 'user', content: 'Hello' }],
      };
      const mockResponse = {
        content: 'Hi there!',
      };
      mockLlmModel.invoke = jest.fn().mockResolvedValue(mockResponse);
      const result = await service.agentChat(singleMessageDto);
      expect(mockLlmModel.invoke).toHaveBeenCalledWith({
        input: 'Hello',
        chat_history: [],
        agent_scratchpad: [],
      });
      expect(result).toBe('Hi there!');
    });

    it('should throw AiAgentServerException when llmModel.invoke fails', async () => {
      const mockError = new Error('Model error');
      mockLlmModel.invoke = jest.fn().mockRejectedValue(mockError);
      await expect(
        service.agentChat(mockContextAwareMessagesDto),
      ).rejects.toThrow(AiAgentServerException);
      expect(mockLogger.error).toHaveBeenCalledWith(
        `[agentChat] Failed. Messages: ${JSON.stringify(
          mockContextAwareMessagesDto.messages,
        )}. Custom Prompt Used: No. Error: ${mockError.message}`,
        mockError.stack,
        'LangchainChatService',
      );
    });

    it('should throw AiAgentServerException when custom prompt is used and error occurs', async () => {
      const customPrompt = 'Custom prompt';
      const mockError = new Error('Model error');
      mockLlmModel.invoke = jest.fn().mockRejectedValue(mockError);
      await expect(
        service.agentChat(mockContextAwareMessagesDto, customPrompt),
      ).rejects.toThrow(AiAgentServerException);
      expect(mockLogger.error).toHaveBeenCalledWith(
        `[agentChat] Failed. Messages: ${JSON.stringify(
          mockContextAwareMessagesDto.messages,
        )}. Custom Prompt Used: Yes. Error: ${mockError.message}`,
        mockError.stack,
        'LangchainChatService',
      );
    });
  });

  describe.skip('private methods', () => {
    describe('successResponse', () => {
      it('should convert Uint8Array to string correctly', () => {
        const uint8Array = new Uint8Array([72, 101, 108, 108, 111]);
        const result = (service as any).successResponse(uint8Array);
        expect(result).toBe('Hello');
      });

      it('should handle empty Uint8Array', () => {
        const uint8Array = new Uint8Array([]);
        const result = (service as any).successResponse(uint8Array);
        expect(result).toBe('');
      });
    });

    describe('loadSingleChain', () => {
      it('should create chain with correct template', () => {
        const template = 'Test template: {input}';
        const mockPipe = jest.fn().mockReturnValue({
          invoke: jest.fn(),
        });
        mockLlmModel.pipe = mockPipe;
        const result = (service as any).loadSingleChain(template);
        expect(mockPipe).toHaveBeenCalled();
        expect(result).toBeDefined();
      });
    });

    describe('formateMessage', () => {
      it('should format message correctly', () => {
        const message = { role: 'user', content: 'Hello' };
        const result = (service as any).formateMessage(message);
        expect(result).toBe('user: Hello');
      });
    });

    describe('formatBaseMessages', () => {
      it('should create HumanMessage for user role', () => {
        const message = { role: VERCEL_ROLES.user, content: 'Hello' };
        const result = (service as any).formatBaseMessages(message);
        expect(result).toBeInstanceOf(HumanMessage);
        expect(result.content).toBe('Hello');
      });

      it('should create AIMessage for assistant role', () => {
        const message = { role: VERCEL_ROLES.assistant, content: 'Hi there!' };
        const result = (service as any).formatBaseMessages(message);
        expect(result).toBeInstanceOf(AIMessage);
        expect(result.content).toBe('Hi there!');
      });

      it('should create HumanMessage for unknown role', () => {
        const message = { role: 'unknown', content: 'Test' };
        const result = (service as any).formatBaseMessages(message);
        expect(result).toBeInstanceOf(HumanMessage);
        expect(result.content).toBe('Test');
      });
    });
  });

  describe.skip('VERCEL_ROLES enum', () => {
    it('should have correct values', () => {
      expect(VERCEL_ROLES.user).toBe('user');
      expect(VERCEL_ROLES.assistant).toBe('assistant');
    });
  });

  describe.skip('edge cases', () => {
    it('should handle null messages in contextAwareChat', async () => {
      const nullMessagesDto: ContextAwareMessagesDto = {
        messages: null as any,
      };
      const mockResponse = new Uint8Array([79, 75]);
      const mockChain = {
        invoke: jest.fn().mockResolvedValue(mockResponse),
      };
      jest.spyOn(service as any, 'loadSingleChain').mockReturnValue(mockChain);
      const result = await service.contextAwareChat(nullMessagesDto);
      expect(mockChain.invoke).toHaveBeenCalledWith({
        chat_history: '',
        input: undefined,
      });
      expect(result).toBe('OK');
    });

    it('should handle null messages in agentChat', async () => {
      const nullMessagesDto: ContextAwareMessagesDto = {
        messages: null as any,
      };
      const mockResponse = {
        content: 'Response',
      };
      mockLlmModel.invoke = jest.fn().mockResolvedValue(mockResponse);
      const result = await service.agentChat(nullMessagesDto);
      expect(mockLlmModel.invoke).toHaveBeenCalledWith({
        input: undefined,
        chat_history: [],
        agent_scratchpad: [],
      });
      expect(result).toBe('Response');
    });

    it('should handle undefined content in messages', async () => {
      const messagesWithUndefinedContent: ContextAwareMessagesDto = {
        messages: [
          { role: 'user', content: undefined as any },
          { role: 'assistant', content: 'Response' },
        ],
      };
      const mockResponse = {
        content: 'Final response',
      };
      mockLlmModel.invoke = jest.fn().mockResolvedValue(mockResponse);
      const result = await service.agentChat(messagesWithUndefinedContent);
      expect(mockLlmModel.invoke).toHaveBeenCalledWith({
        input: undefined,
        chat_history: [
          new HumanMessage({ content: '', additional_kwargs: {} }),
        ],
        agent_scratchpad: [],
      });
      expect(result).toBe('Final response');
    });
  });
});
