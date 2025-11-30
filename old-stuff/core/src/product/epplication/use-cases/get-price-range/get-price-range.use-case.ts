import { Injectable } from '@nestjs/common';
import { GetPriceRangeDto } from 'src/product/interface/dtos/get-price-range.dto';
import { IGetPriceRangeUseCase } from './get-price-range.interface';
import { LangchainChatService } from 'src/ai-chatbot/services/langchain-chat/langchain-chat.service';
import { ContextAwareMessagesDto } from 'src/ai-chatbot/interfaces/dtos/context-aware-message.dto';
import { IPromptBuilderService } from 'src/shared/prompt-builder/prompt-builder.interface';
import { MessageContent } from '@langchain/core/messages';

@Injectable()
export class GetPriceRangeUseCase implements IGetPriceRangeUseCase {
  constructor(
    private readonly langchainChatService: LangchainChatService,
    private readonly promptBuilderService: IPromptBuilderService,
  ) {}

  async execute(dto: GetPriceRangeDto): Promise<MessageContent> {
    const customPrompt = this.promptBuilderService.buildPriceRange(dto);

    const messages: ContextAwareMessagesDto = {
      messages: [
        {
          role: 'user',
          content: `Please analyze the pricing for this product: ${dto.title}

          Product Title: ${dto.title}
          Description: ${dto.description}
          Price: $${(dto.price / 100).toFixed(2)}`,
        },
      ],
    };

    const response = await this.langchainChatService.agentChat(
      messages,
      customPrompt,
    );

    return response;
  }
}
