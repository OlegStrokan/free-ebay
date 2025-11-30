import { Product } from 'src/product/core/product/entity/product';

export abstract class IMarkAsOutOfStockUseCase {
  abstract execute(productId: string): Promise<Product>;
}
