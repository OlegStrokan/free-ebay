import { Injectable } from '@nestjs/common';
import { IPromptBuilderService } from './prompt-builder.interface';
import { GetPriceRangeDto } from 'src/product/interface/dtos/get-price-range.dto';

@Injectable()
export class PromptBuilderService implements IPromptBuilderService {
  buildPriceRange(dto: GetPriceRangeDto): string {
    const priceInDollars = (dto.price / 100).toFixed(2);

    return `You are a pricing expert and market analyst. Analyze the following product and provide a comprehensive price assessment:
    
          Product Title: ${dto.title}
          Product Description: ${dto.description}
          Current Price: $${priceInDollars}
    
          Your task is to:
          1. Research current market prices for similar products
          2. Analyze the product features and market positioning
          3. Provide a reasonable price range (minimum and maximum)
          4. Give a clear conclusion about whether the current price is:
            - "REASONABLE" - if it's within the expected range
            - "HIGH" - if it's above the expected range
            - "LOW" - if it's below the expected range
    
          In response you should return just:
          Price Range: $X.XX - $Y.YY
          Conclusion: [REASONABLE/HIGH/LOW]
          Analysis: [Brief explanation of your reasoning]
    
          Be thorough in your market research and provide accurate, data-driven insights.`;
  }
}
