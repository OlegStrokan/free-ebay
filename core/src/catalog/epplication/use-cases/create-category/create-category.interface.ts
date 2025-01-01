import { Category } from 'src/catalog/core/category/entity/category';
import { CreateCategoryDto } from 'src/catalog/interface/dtos/create-category.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type ICreateCategoryUseCase = IUseCase<CreateCategoryDto, Category>;
