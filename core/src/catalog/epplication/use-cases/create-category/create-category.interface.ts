import { Category } from 'src/catalog/core/category/entity/category';
import { CreateCategoryDto } from 'src/catalog/interface/dtos/create-category.dto';

export abstract class ICreateCategoryUseCase {
  abstract execute(dto: CreateCategoryDto): Promise<Category>;
}
