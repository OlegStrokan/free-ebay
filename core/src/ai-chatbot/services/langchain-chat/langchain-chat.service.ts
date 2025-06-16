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

import {
  AIMessage,
  BaseMessageChunk,
  HumanMessage,
} from '@langchain/core/messages';
import { RunnableLike } from '@langchain/core/runnables';
import { AI_TEMPLATES } from 'src/ai-chatbot/utils/constants/templates.constants';
import { customMessage } from 'src/ai-chatbot/utils/responses/custom-message.response';
import { ContextAwareMessagesDto } from 'src/ai-chatbot/interfaces/dtos/context-aware-message.dto';
import { TavilySearchResults } from '@langchain/community/tools/tavily_search';

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
      const tools = [new TavilySearchResults({ maxResults: 1 })];

      const messages = dto.messages ?? [];
      const formattedPreviousMessages = messages
        .slice(0, -1)
        .map(this.formatBaseMessages);

      const currentMessageContent = messages[messages.length - 1].content;

      const prompt = ChatPromptTemplate.fromMessages([
        [
          'system',
          'You are an agent that follows SI system standards and responds normally ',
        ],
        new MessagesPlaceholder({ variableName: 'chat_history ' }),
        ['user', '{input}'],
        new MessagesPlaceholder({ variableName: 'agent_scratchpad' }),
      ]);

      const llm = new ChatOpenAI({
        temperature: 0.8,
        model: 'gpt-4-turbo',
      });

      //@fix delete ts-ignore
      const agent = await createOpenAIFunctionsAgent({
        llm,
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        //@ts-ignore
        tools,
        prompt,
      });

      const agentExecutor = new AgentExecutor({
        agent,
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        //@ts-ignore
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
      model: 'gpt-4-turbo',
      temperature: 0.8,
    });

    const outputParser =
      new HttpResponseOutputParser() as unknown as RunnableLike<
        BaseMessageChunk,
        Uint8Array
      >;

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
    throw new HttpException(
      customMessage(HttpStatus.INTERNAL_SERVER_ERROR, 'server-error'),
      HttpStatus.INTERNAL_SERVER_ERROR,
    );
  }

  private formateMessage(message: Message) {
    return `${message.role}: ${message.content}`;
  }

  private formatBaseMessages(message: Message) {
    message.role === vercelRoles.user
      ? new HumanMessage({ content: message.content, additional_kwargs: {} })
      : new AIMessage({ content: message.content, additional_kwargs: {} });
  }
}
