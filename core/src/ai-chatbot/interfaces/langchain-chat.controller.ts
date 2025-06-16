import { Body, Controller, Post } from '@nestjs/common';
import { ChatCompletionDto } from './dtos/chat-completion.dto';
import { OpenAiChatbotService } from '../services/open-ai/open-ai-chatbot.service';
import { LocalAiChatbotService } from '../services/local-ai/local-ai-chatbot.service';

@Controller('ai-chatbot')
export class LangchainChatController {
  constructor(
    private readonly openAiService: OpenAiChatbotService,
    private readonly localAiService: LocalAiChatbotService,
  ) {}

  @Post('chat')
  async createChatCompletion(@Body() body: ChatCompletionDto) {
    return this.openAiService.createChatCompletion(body.messages);
  }

  @Post('local-chat')
  async createLocalChatCompletion(@Body() body: ChatCompletionDto) {
    return this.localAiService.createChatCompletion(body.messages);
  }
}
