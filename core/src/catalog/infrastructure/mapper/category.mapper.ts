import { Inject, Injectable } from '@nestjs/common';
import { Category } from 'src/catalog/core/category/entity/category';
import { CategoryDb } from '../entity/category';
import { CategoryData } from 'src/catalog/core/category/entity/category';
import { ICategoryMapper } from './category.mapper.interface';
import { IProductMapper } from 'src/product/infrastructure/mappers/product/product.mapper.interface';
import { ProductData } from 'src/product/core/product/entity/product.interface';
import { Product } from 'src/product/core/product/entity/product';
import { ProductDb } from 'src/product/infrastructure/entity/product.entity';
import { PRODUCT_MAPPER } from 'src/product/epplication/injection-tokens/mapper.token';

@Injectable()
export class CategoryMapper
  implements ICategoryMapper<CategoryData, Category, CategoryDb>
{
  constructor(
    @Inject(PRODUCT_MAPPER)
    private readonly productMapper: IProductMapper<
      ProductData,
      Product,
      ProductDb
    >,
  ) {}
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

    categoryDb.products =
      products.length > 0
        ? products.map((product) => this.productMapper.toDb(product))
        : [];

    return categoryDb;
  }

  toDomain({
    id,
    children,
    description,
    name,
    products,
    parentCategory,
  }: CategoryDb): Category {
    const categoryData: CategoryData = {
      id,
      name,
      description,
      parentCategoryId: parentCategory ? parentCategory.id : undefined,
      children: children?.map((child) => this.toDomain(child)),
      products:
        products?.length > 0
          ? products.map((product) => this.productMapper.toDomain(product))
          : [],
    };
    return new Category(categoryData);
  }

  toClient({ data }: Category): CategoryData {
    return data;
  }
}
