import { Injectable } from '@nestjs/common';
import { Category } from 'src/catalog/core/category/entity/category';
import { CategoryDb } from '../entity/category';
import { CategoryData } from 'src/catalog/core/category/entity/category';
import { ICategoryMapper } from './category.mapper.interface';

@Injectable()
export class CategoryMapper
  implements ICategoryMapper<CategoryData, Category, CategoryDb>
{
  toDb({
    id,
    name,
    description,
    parentCategoryId,
    children,
    products,
  }: Category): CategoryDb {
    const categoryDb = new CategoryDb();
    categoryDb.id = id;
    categoryDb.name = name;
    categoryDb.description = description;
    categoryDb.parentCategory = undefined;
    if (parentCategoryId) {
      const parentCategory = new CategoryDb();
      parentCategory.id = parentCategoryId;
      categoryDb.parentCategory = parentCategory;
    }

    categoryDb.children = children?.map((child) => this.toDb(child));

    categoryDb.products = products;

    return categoryDb;
  }

  toDomain(categoryDb: CategoryDb): Category {
    const categoryData: CategoryData = {
      id: categoryDb.id,
      name: categoryDb.name,
      description: categoryDb.description,
      parentCategoryId: categoryDb.parentCategory
        ? categoryDb.parentCategory.id
        : undefined,
      children: categoryDb.children?.map((child) => this.toDomain(child)),
      products: categoryDb.products,
    };
    return new Category(categoryData);
  }

  toClient({ data }: Category): CategoryData {
    return data;
  }
}
