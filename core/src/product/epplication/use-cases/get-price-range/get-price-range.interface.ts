import { MessageContent } from '@langchain/core/messages';
import { GetPriceRangeDto } from 'src/product/interface/dtos/get-price-range.dto';

export abstract class IGetPriceRangeUseCase {
  abstract execute(dto: GetPriceRangeDto): Promise<MessageContent>;
}
