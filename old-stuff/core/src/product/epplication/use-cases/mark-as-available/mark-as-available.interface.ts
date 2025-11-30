import { Product } from 'src/product/core/product/entity/product';

export abstract class IMarkAsAvailableUseCase {
  abstract execute(productId: string): Promise<Product>;
}
