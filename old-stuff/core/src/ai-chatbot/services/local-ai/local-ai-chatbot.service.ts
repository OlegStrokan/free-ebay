import { Injectable } from '@nestjs/common';
import axios from 'axios';
import { ChatCompletionMessageDto } from 'src/ai-chatbot/interfaces/dtos/chat-completion.dto';

@Injectable()
export class LocalAiChatbotService {
  constructor(private readonly ollamaApiUrl: string) {}

  async createChatCompletion(messages: ChatCompletionMessageDto[]) {
    const ollamaMessages = messages.map((message) => ({
      role: message.role,
      content: message.content,
    }));

    try {
      const response = await axios.post(this.ollamaApiUrl, {
        model: 'deepseek-r1:1.5b',
        messages: ollamaMessages,
        stream: false,
      });

      return {
        id: response.data.id,
        choices: [
          {
            message: {
              role: 'assistant',
              content: response.data.message?.content || '',
            },
          },
        ],
      };
    } catch (error) {
      console.log(error);
      throw new Error(`Local AI request failed: ${error}`);
    }
  }
}
