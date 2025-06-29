import { GetPriceRangeDto } from 'src/product/interface/dtos/get-price-range.dto';

export abstract class IPromptBuilderService {
  abstract buildPriceRange(dto: GetPriceRangeDto): string;
}
