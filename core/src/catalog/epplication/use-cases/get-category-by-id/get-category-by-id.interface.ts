import {
  Category,
  CategoryData,
} from 'src/catalog/core/category/entity/category';

export abstract class IGetCategoryByIdUseCase {
  abstract execute(categoryId: CategoryData['id']): Promise<Category>;
}
