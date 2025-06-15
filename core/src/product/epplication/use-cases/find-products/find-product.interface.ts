import { Product } from 'src/product/core/product/entity/product';

export abstract class IFindProductsUseCase {
  abstract execute(): Promise<Product[]>;
}
