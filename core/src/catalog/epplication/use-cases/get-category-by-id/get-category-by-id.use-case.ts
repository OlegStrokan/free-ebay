import { Inject } from '@nestjs/common';
import { Category } from 'src/catalog/core/category/entity/category';
import { CategoryNotFoundException } from 'src/catalog/core/category/entity/exceptions/category-not-found.exception';
import {
  CATEGORY_REPOSITORY,
  ICategoryRepository,
} from 'src/catalog/core/category/repository/category.repository';
import { IGetCategoryByIdUseCase } from './get-category-by-id.interface';

export class GetCategoryByIdUseCase implements IGetCategoryByIdUseCase {
  constructor(
    @Inject(CATEGORY_REPOSITORY)
    private readonly categoryRepository: ICategoryRepository,
  ) {}

  public async execute(id: string): Promise<Category> {
    const category = await this.categoryRepository.findById(id);
    if (!category) {
      throw new CategoryNotFoundException('id', id);
    }
    return category;
  }
}
