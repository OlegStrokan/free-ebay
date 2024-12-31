import { Product } from 'src/product/core/product/entity/product';
import { ProductData } from 'src/product/core/product/entity/product.interface';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IFindProductUseCase = IUseCase<ProductData['id'], Product>;
