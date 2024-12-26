import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { Product } from 'src/product/core/product/entity/product';

export interface IProductMockService {
  getOneToCreate(): CreateProductDto;
  getOne(): Product;
  createOne(): Promise<Product>;
}
