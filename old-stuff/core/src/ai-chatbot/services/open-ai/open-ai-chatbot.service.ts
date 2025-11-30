import { Injectable } from '@nestjs/common';
import OpenAI from 'openai';
import { ChatCompletionMessageDto } from 'src/ai-chatbot/interfaces/dtos/chat-completion.dto';
import { ChatCompletionMessageParam } from 'openai/resources';

@Injectable()
export class OpenAiChatbotService {
  constructor(private readonly openai: OpenAI) {}

  async createChatCompletion(messages: ChatCompletionMessageDto[]) {
    return this.openai.chat.completions.create({
      messages: messages as ChatCompletionMessageParam[],
      model: 'gpt-4.1-nano',
    });
  }
}
