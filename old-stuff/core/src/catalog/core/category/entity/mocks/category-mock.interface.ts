import { ProductData } from 'src/product/core/product/entity/product.interface';
import { CategoryData, Category } from '../category';
import { CreateCategoryDto } from 'src/catalog/interface/dtos/create-category.dto';

export abstract class ICategoryMockService {
  abstract getOneToCreate(overrides?: Partial<CategoryData>): CreateCategoryDto;
  abstract getOne(overrides?: Partial<CategoryData>): Category;
  abstract createOne(overrides?: Partial<CategoryData>): Promise<Category>;
  abstract createOneWithDepencencies(
    categoryOverrides?: Partial<CategoryData>,
    productOverrides?: Partial<ProductData>,
  ): Promise<Category>;
}
