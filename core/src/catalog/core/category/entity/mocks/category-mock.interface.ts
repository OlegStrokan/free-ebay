import { ProductData } from 'src/product/core/product/entity/product.interface';
import { CategoryData, Category } from '../category';
import { CreateCategoryDto } from 'src/catalog/interface/dtos/create-category.dto';

export interface ICategoryMockService {
  getOneToCreate(overrides?: Partial<CategoryData>): CreateCategoryDto;
  getOne(overrides?: Partial<CategoryData>): Category;
  createOne(overrides?: Partial<CategoryData>): Promise<Category>;
  createOneWithDepencencies(
    categoryOverrides?: Partial<CategoryData>,
    productOverrides?: Partial<ProductData>,
  ): Promise<Category>;
}
