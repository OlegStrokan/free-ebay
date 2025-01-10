import { faker } from '@faker-js/faker';
import { Injectable, Inject } from '@nestjs/common';
import { CategoryData, Category } from '../category';
import { ICategoryRepository } from '../../repository/category.repository';
import { CreateCategoryDto } from 'src/catalog/interface/dtos/create-category.dto';
import { ICategoryMockService } from './category-mock.interface';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductData } from 'src/product/core/product/entity/product.interface';
import { CATEGORY_REPOSITORY } from 'src/catalog/epplication/injection-tokens/repository.token';
import { PRODUCT_MOCK_SERVICE } from 'src/product/epplication/injection-tokens/mock-services.token';

@Injectable()
export class CategoryMockService implements ICategoryMockService {
  constructor(
    @Inject(CATEGORY_REPOSITORY)
    private readonly categoryRepository: ICategoryRepository,
    @Inject(PRODUCT_MOCK_SERVICE)
    private readonly productMockService: IProductMockService,
  ) {}

  getOneToCreate(overrides: Partial<CategoryData> = {}): CreateCategoryDto {
    return {
      name: overrides.name ?? faker.commerce.department(),
      description: overrides.description ?? faker.commerce.productDescription(),
    };
  }

  getOne(overrides: Partial<CategoryData> = {}): Category {
    return Category.create({
      name: overrides.name ?? faker.commerce.department(),
      description: overrides.description ?? faker.commerce.productDescription(),
      products: overrides.products ?? [],
    });
  }

  async createOne(overrides: Partial<CategoryData> = {}): Promise<Category> {
    const category = this.getOne(overrides);
    return await this.categoryRepository.save(category);
  }

  async createOneWithDepencencies(
    categoryOverrides: Partial<CategoryData> = {},
    productOverrides: Partial<ProductData> = {},
  ): Promise<Category> {
    const category = this.getOne(categoryOverrides);
    await this.productMockService.createOne({ ...productOverrides, category });
    return await this.categoryRepository.save(category);
  }
}
