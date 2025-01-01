import { Category } from 'src/catalog/core/category/entity/category';
import { IUseCase } from 'src/shared/types/use-case.interface';
import { UpdateCategoryRequest } from './update-category.use-case';

export type IUpdateCategoryUseCase = IUseCase<UpdateCategoryRequest, Category>;
