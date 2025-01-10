import { Inject } from '@nestjs/common';
import { Category } from 'src/catalog/core/category/entity/category';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { IGetAllCategoriesUseCase } from './get-all-categories.interface';
import { CATEGORY_REPOSITORY } from '../../injection-tokens/repository.token';

export class GetAllCategoriesUseCase implements IGetAllCategoriesUseCase {
  constructor(
    @Inject(CATEGORY_REPOSITORY)
    private readonly categoryRepository: ICategoryRepository,
  ) {}

  public async execute(): Promise<Category[]> {
    return this.categoryRepository.findAll(1, 100);
  }
}
