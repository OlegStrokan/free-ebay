import { Module } from '@nestjs/common';
import { AiChatBotController } from './interfaces/ai-chatbot.controller';
import { ConfigModule, ConfigService } from '@nestjs/config';
import OpenAI from 'openai';
import { OpenAiChatbotService } from './services/open-ai/open-ai-chatbot.service';
import { HttpModule } from '@nestjs/axios';
import { LocalAiChatbotService } from './services/local-ai/local-ai-chatbot.service';

@Module({
  controllers: [AiChatBotController],
  imports: [ConfigModule, HttpModule],
  providers: [
    OpenAiChatbotService,
    LocalAiChatbotService,
    {
      provide: LocalAiChatbotService,
      useFactory: (configService: ConfigService) => {
        return new LocalAiChatbotService(
          configService.getOrThrow('OLLAMA_API_URL'),
        );
      },
      inject: [ConfigService],
    },
    {
      provide: OpenAI,
      useFactory: (configService: ConfigService) =>
        new OpenAI({
          apiKey: configService.getOrThrow('OPEN_AI_API_KEY'),
          project: configService.getOrThrow('OPEN_AI_PROJECT_ID'),
          organization: configService.getOrThrow('OPEN_AI_ORGANIZATION_ID'),
        }),
      inject: [ConfigService],
    },
  ],
})
export class AiChatBotModule {}
