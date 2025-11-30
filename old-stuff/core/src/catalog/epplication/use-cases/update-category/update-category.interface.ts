import { Category } from 'src/catalog/core/category/entity/category';
import { UpdateCategoryRequest } from './update-category.use-case';

export abstract class IUpdateCategoryUseCase {
  abstract execute(request: UpdateCategoryRequest): Promise<Category>;
}
