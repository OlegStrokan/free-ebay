import { HttpException, HttpStatus, Injectable } from '@nestjs/common';
import { UserMessageDto } from 'src/ai-chatbot/interfaces/dtos/user-message.dto';
import {
  ChatPromptTemplate,
  MessagesPlaceholder,
  PromptTemplate,
} from '@langchain/core/prompts';
import { ChatOpenAI } from '@langchain/openai';
import { HttpResponseOutputParser } from 'langchain/output_parsers';
import { AgentExecutor, createOpenAIFunctionsAgent } from 'langchain/agents';

import { AIMessage, HumanMessage } from '@langchain/core/messages';
import { AI_TEMPLATES } from 'src/ai-chatbot/utils/constants/templates.constants';
import { customMessage } from 'src/ai-chatbot/utils/responses/custom-message.response';
import { ContextAwareMessagesDto } from 'src/ai-chatbot/interfaces/dtos/context-aware-message.dto';
import { TavilySearch } from '@langchain/tavily';

// copied from 'ai' vercel library
// @fix remove shit for 10 lines below
interface Message {
  role: string;
  content: string;
}

export enum vercelRoles {
  user = 'user',
  assistant = 'assistant',
}

@Injectable()
export class LangchainChatService {
  constructor(
    private readonly tavilySearch: TavilySearch,
    private readonly chatOpenAI: ChatOpenAI,
  ) {}

  async basicChat(dto: UserMessageDto) {
    try {
      const chain = this.loadSingleChain(AI_TEMPLATES.BASIC_CHAT_TEMPLATE);
      const response = await chain.invoke({
        input: dto.query,
      });
      return this.successResponse(response);
    } catch (e: unknown) {
      this.exceptionHandling(e);
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
      this.exceptionHandling(e);
    }
  }

  async agentChat(dto: ContextAwareMessagesDto) {
    try {
      const tools = [
        new TavilySearch({
          ...this.tavilySearch,
          maxResults: 1,
        }),
      ];

      const messages = dto.messages ?? [];
      const formattedPreviousMessages = messages
        .slice(0, -1)
        .map(this.formatBaseMessages);

      const currentMessageContent = messages[messages.length - 1].content;

      const prompt = ChatPromptTemplate.fromMessages([
        ['system', 'you is doctor agent. you answer only on medical question'],
        new MessagesPlaceholder({ variableName: 'chat_history' }),
        ['user', '{input}'],
        new MessagesPlaceholder({ variableName: 'agent_scratchpad' }),
      ]);

      const llm = new ChatOpenAI({
        ...this.chatOpenAI,
        temperature: 0.8,
        model: 'gpt-4.1-nano',
      });

      const agent = await createOpenAIFunctionsAgent({
        llm,
        tools,
        prompt,
      });

      const agentExecutor = new AgentExecutor({
        agent,
        tools,
      });

      const response = await agentExecutor.invoke({
        input: currentMessageContent,
        chat_history: formattedPreviousMessages,
      });

      return customMessage(HttpStatus.OK, 'success', response.output);
    } catch (e: unknown) {
      this.exceptionHandling(e);
    }
  }

  private loadSingleChain(template: string) {
    const prompt = PromptTemplate.fromTemplate(template);

    const model = new ChatOpenAI({
      ...this.chatOpenAI,
      model: 'gpt-4.1-nano',
      temperature: 0.8,
    });

    const outputParser = new HttpResponseOutputParser();

    return prompt.pipe(model).pipe(outputParser);
  }

  //@fix - remove this shit. service shoudn't handle this type of logic
  private successResponse(response: Uint8Array) {
    customMessage(
      HttpStatus.OK,
      'success',
      Object.values(response)
        .map((code) => String.fromCharCode(code))
        .join(''),
    );
  }

  private exceptionHandling(e: unknown): never {
    console.error('error', e);
    throw new HttpException(
      customMessage(HttpStatus.INTERNAL_SERVER_ERROR, 'server-error'),
      HttpStatus.INTERNAL_SERVER_ERROR,
    );
  }

  private formateMessage(message: Message) {
    return `${message.role}: ${message.content}`;
  }

  private formatBaseMessages(message: Message) {
    return message.role === vercelRoles.user
      ? new HumanMessage({ content: message.content, additional_kwargs: {} })
      : new AIMessage({ content: message.content, additional_kwargs: {} });
  }
}
