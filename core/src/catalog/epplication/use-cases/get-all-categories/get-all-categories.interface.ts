import { Category } from 'src/catalog/core/category/entity/category';

export abstract class IGetAllCategoriesUseCase {
  abstract execute(): Promise<Category[]>;
}
