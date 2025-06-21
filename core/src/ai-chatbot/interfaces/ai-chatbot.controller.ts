import { Body, Controller, Post } from '@nestjs/common';
import { ChatCompletionDto } from './dtos/chat-completion.dto';
import { OpenAiChatbotService } from '../services/open-ai/open-ai-chatbot.service';
import { LocalAiChatbotService } from '../services/local-ai/local-ai-chatbot.service';
import { LangchainChatService } from '../services/langchain-chat/langchain-chat.service';
import { ContextAwareMessagesDto } from './dtos/context-aware-message.dto';

@Controller('ai-chatbot')
export class AiChatBotController {
  constructor(
    private readonly openAiService: OpenAiChatbotService,
    private readonly localAiService: LocalAiChatbotService,
    private readonly langchainChatService: LangchainChatService,
  ) {}

  @Post('chat')
  async createChatCompletion(@Body() body: ChatCompletionDto) {
    return this.openAiService.createChatCompletion(body.messages);
  }

  @Post('local-chat')
  async createLocalChatCompletion(@Body() body: ChatCompletionDto) {
    return this.localAiService.createChatCompletion(body.messages);
  }

  @Post('langchain-chat')
  async createLangchainChatCompletion(@Body() body: ContextAwareMessagesDto) {
    return this.langchainChatService.agentChat(body);
  }
}
