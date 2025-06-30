import { Product } from 'src/product/core/product/entity/product';
import { PaginatedResult } from 'src/shared/types/paginated-result';
import { FindProductsRequestDto } from './find-products.use-case';

export abstract class IFindProductsUseCase {
  abstract execute(
    dto: FindProductsRequestDto,
  ): Promise<PaginatedResult<Product>>;
}
