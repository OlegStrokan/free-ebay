import { Inject } from '@nestjs/common';
import { IDeleteCategoryUseCase } from './delete-category.interface';
import {
  CATEGORY_REPOSITORY,
  ICategoryRepository,
} from 'src/catalog/core/category/repository/category.repository';

export class DeleteCategoryUseCase implements IDeleteCategoryUseCase {
  constructor(
    @Inject(CATEGORY_REPOSITORY)
    private readonly categoryRepository: ICategoryRepository,
  ) {}

  public async execute(id: string): Promise<void> {
    return await this.categoryRepository.deleteById(id);
  }
}
