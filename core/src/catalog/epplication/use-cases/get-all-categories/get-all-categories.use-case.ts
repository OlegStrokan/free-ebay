import { Inject } from '@nestjs/common';
import { Category } from 'src/catalog/core/category/entity/category';
import {
  CATEGORY_REPOSITORY,
  ICategoryRepository,
} from 'src/catalog/core/category/repository/category.repository';

export class GetAllCategoriesUseCase implements GetAllCategoriesUseCase {
  constructor(
    @Inject(CATEGORY_REPOSITORY)
    private readonly categoryRepository: ICategoryRepository,
  ) {}

  public async execute(): Promise<Category[]> {
    return this.categoryRepository.findAll(0, 100);
  }
}
