import { forwardRef, Inject, Injectable } from '@nestjs/common';
import { Category } from 'src/catalog/core/category/entity/category';
import { CategoryDb } from '../entity/category.entity';
import { CategoryData } from 'src/catalog/core/category/entity/category';
import { ICategoryMapper } from './category.mapper.interface';
import { IProductMapper } from 'src/product/infrastructure/mappers/product/product.mapper.interface';
import { CategoryDto } from 'src/catalog/interface/dtos/category.dto';

@Injectable()
export class CategoryMapper implements ICategoryMapper {
  constructor(
    @Inject(forwardRef(() => IProductMapper))
    private readonly productMapper: IProductMapper,
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

  toClient(categoryData: CategoryData): CategoryDto {
    const childrenDto = categoryData.children.map((child) =>
      this.toClient(child),
    );
    const productsDto = categoryData.products.map((product) =>
      this.productMapper.toClient(product),
    );

    return {
      id: categoryData.id,
      name: categoryData.name,
      description: categoryData.description,
      parentCategoryId: categoryData.parentCategoryId,
      children: childrenDto,
      products: productsDto,
    };
  }
}
