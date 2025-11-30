import { Injectable } from '@nestjs/common';
import { IDeleteCategoryUseCase } from './delete-category.interface';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { CategoryData } from 'src/catalog/core/category/entity/category';

@Injectable()
export class DeleteCategoryUseCase implements IDeleteCategoryUseCase {
  constructor(private readonly categoryRepository: ICategoryRepository) {}

  public async execute(id: CategoryData['id']): Promise<void> {
    return await this.categoryRepository.deleteById(id);
  }
}
