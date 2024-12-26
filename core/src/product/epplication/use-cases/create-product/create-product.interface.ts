import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type ICreateProductUseCase = IUseCase<CreateProductDto, void>;
