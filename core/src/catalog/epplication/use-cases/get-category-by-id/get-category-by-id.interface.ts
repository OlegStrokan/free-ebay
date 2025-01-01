import { Category } from 'src/catalog/core/category/entity/category';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IGetCategoryByIdUseCase = IUseCase<string, Category>;
