import { CategoryData } from 'src/catalog/core/category/entity/category';

export abstract class IDeleteCategoryUseCase {
  abstract execute(categoryId: CategoryData['id']): Promise<void>;
}
