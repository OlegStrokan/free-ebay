import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { Product } from 'src/product/core/product/entity/product';
import { ProductData } from '../product.interface';

export interface IProductMockService {
  getOneToCreate(): CreateProductDto;
  getOne(overrides?: Partial<ProductData>): Product;
  createOne(overrides?: Partial<ProductData>): Promise<Product>;
}
