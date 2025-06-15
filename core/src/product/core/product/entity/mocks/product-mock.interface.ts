import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { Product } from 'src/product/core/product/entity/product';
import { ProductData } from '../product.interface';

export abstract class IProductMockService {
  abstract getOneToCreate(): CreateProductDto;
  abstract getOne(overrides?: Partial<ProductData>): Product;
  abstract createOne(overrides?: Partial<ProductData>): Promise<Product>;
}
