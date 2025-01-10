import { Inject } from '@nestjs/common';
import { IDeleteCategoryUseCase } from './delete-category.interface';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { CATEGORY_REPOSITORY } from '../../injection-tokens/repository.token';

export class DeleteCategoryUseCase implements IDeleteCategoryUseCase {
  constructor(
    @Inject(CATEGORY_REPOSITORY)
    private readonly categoryRepository: ICategoryRepository,
  ) {}

  public async execute(id: string): Promise<void> {
    return await this.categoryRepository.deleteById(id);
  }
}
