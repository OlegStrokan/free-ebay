import { Product } from 'src/product/core/product/entity/product';
import { ProductData } from 'src/product/core/product/entity/product.interface';

export abstract class IFindProductUseCase {
  abstract execute(productId: ProductData['id']): Promise<Product>;
}
