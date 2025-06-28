import { Injectable, Logger } from '@nestjs/common'; // Import Logger
import { UserMessageDto } from 'src/ai-chatbot/interfaces/dtos/user-message.dto';
import {
  ChatPromptTemplate,
  MessagesPlaceholder,
  PromptTemplate,
} from '@langchain/core/prompts';
import { ChatOpenAI } from '@langchain/openai';
import { HttpResponseOutputParser } from 'langchain/output_parsers';
import { AIMessage, HumanMessage } from '@langchain/core/messages';
import { AI_TEMPLATES } from 'src/ai-chatbot/utils/constants/templates.constants';
import { ContextAwareMessagesDto } from 'src/ai-chatbot/interfaces/dtos/context-aware-message.dto';
import { AiAgentServerException } from 'src/product/core/product/exceptions/ai-agent-server.exception';

interface Message {
  role: string;
  content: string;
}

export enum VERCEL_ROLES {
  user = 'user',
  assistant = 'assistant',
}

@Injectable()
export class LangchainChatService {
  constructor(
    private readonly llmModel: ChatOpenAI,
    private readonly logger: Logger,
  ) {}

  async basicChat(dto: UserMessageDto) {
    try {
      const chain = this.loadSingleChain(AI_TEMPLATES.BASIC_CHAT_TEMPLATE);
      const response = await chain.invoke({
        input: dto.query,
      });
      return this.successResponse(response);
    } catch (e: unknown) {
      this.logger.error(
        `[basicChat] Failed for query: ${dto.query}. Error: ${
          e instanceof Error ? e.message : String(e)
        }`,
        e instanceof Error ? e.stack : undefined,
        LangchainChatService.name,
      );
      throw new AiAgentServerException();
    }
  }

  async contextAwareChat(dto: ContextAwareMessagesDto) {
    try {
      const messages = dto.messages ?? [];
      const formattedPreviousMessages = messages
        .slice(0, -1)
        .map(this.formateMessage);

      const currentMessageContent = messages[messages.length - 1].content;

      const chain = this.loadSingleChain(
        AI_TEMPLATES.CONTEXT_AWARE_CHAT_TEMPLATE,
      );

      const response = await chain.invoke({
        chat_history: formattedPreviousMessages.join('\n'),
        input: currentMessageContent,
      });
      return this.successResponse(response);
    } catch (e: unknown) {
      this.logger.error(
        `[contextAwareChat] Failed. Messages: ${JSON.stringify(
          dto.messages,
        )}. Error: ${e instanceof Error ? e.message : String(e)}`,
        e instanceof Error ? e.stack : undefined,
        LangchainChatService.name,
      );
      throw new AiAgentServerException();
    }
  }

  async agentChat(dto: ContextAwareMessagesDto, customPrompt?: string) {
    try {
      const messages = dto.messages ?? [];
      const formattedPreviousMessages = messages
        .slice(0, -1)
        .map(this.formatBaseMessages);

      const currentMessageContent = messages[messages.length - 1].content;
      const prompt = ChatPromptTemplate.fromMessages([
        [
          'system',
          customPrompt ||
            'You are a helpful assistant that provides accurate and helpful responses.',
        ],
        new MessagesPlaceholder({ variableName: 'chat_history' }),
        ['user', '{input}'],
        new MessagesPlaceholder({ variableName: 'agent_scratchpad' }),
      ]);

      const chain = prompt.pipe(this.llmModel);
      const response = await chain.invoke({
        input: currentMessageContent,
        chat_history: formattedPreviousMessages,
        agent_scratchpad: [],
      });
      return response.content;
    } catch (e: unknown) {
      this.logger.error(
        `[agentChat] Failed. Messages: ${JSON.stringify(
          dto.messages,
        )}. Custom Prompt Used: ${customPrompt ? 'Yes' : 'No'}. Error: ${
          e instanceof Error ? e.message : String(e)
        }`,
        e instanceof Error ? e.stack : undefined,
        LangchainChatService.name,
      );
      throw new AiAgentServerException();
    }
  }

  private successResponse(response: Uint8Array): string {
    return Object.values(response)
      .map((code) => String.fromCharCode(code))
      .join('');
  }

  private loadSingleChain(template: string) {
    const prompt = PromptTemplate.fromTemplate(template);
    const outputParser = new HttpResponseOutputParser();
    return prompt.pipe(this.llmModel).pipe(outputParser);
  }

  private formateMessage(message: Message) {
    return `${message.role}: ${message.content}`;
  }

  private formatBaseMessages(message: Message) {
    return message.role === VERCEL_ROLES.user
      ? new HumanMessage({ content: message.content, additional_kwargs: {} })
      : new AIMessage({ content: message.content, additional_kwargs: {} });
  }
}
