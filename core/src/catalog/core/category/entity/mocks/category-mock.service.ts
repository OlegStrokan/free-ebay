import { faker } from '@faker-js/faker';
import { Injectable, Inject } from '@nestjs/common';
import { CategoryData, Category } from '../category';
import { ICategoryRepository } from '../../repository/category.repository';
import { CATEGORY_REPOSITORY } from '../../repository/category.repository';
import { CreateCategoryDto } from 'src/catalog/interface/dtos/create-category.dto';
import { ICategoryMockService } from './category-mock.interface';

@Injectable()
export class CategoryMockService implements ICategoryMockService {
  constructor(
    @Inject(CATEGORY_REPOSITORY)
    private readonly categoryRepository: ICategoryRepository,
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
    });
  }

  async createOne(overrides: Partial<CategoryData> = {}): Promise<Category> {
    const category = this.getOne(overrides);
    return await this.categoryRepository.save(category);
  }
}
