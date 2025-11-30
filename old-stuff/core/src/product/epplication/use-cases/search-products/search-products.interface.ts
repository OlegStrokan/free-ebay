import { Product } from 'src/product/core/product/entity/product';

export abstract class ISearchProductsUseCase {
  abstract execute(query: string): Promise<Product[]>;
}
