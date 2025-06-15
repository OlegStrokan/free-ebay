import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';

export abstract class ICreateProductUseCase {
  abstract execute(dto: CreateProductDto): Promise<void>;
}
