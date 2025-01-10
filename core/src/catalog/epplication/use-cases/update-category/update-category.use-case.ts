import { Inject } from '@nestjs/common';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { IUpdateCategoryUseCase } from './update-category.interface';
import { UpdateCategoryDto } from 'src/catalog/interface/dtos/update-category.dto';
import { Category } from 'src/catalog/core/category/entity/category';
import { CategoryNotFoundException } from 'src/catalog/core/category/entity/exceptions/category-not-found.exception';
import { CATEGORY_REPOSITORY } from '../../injection-tokens/repository.token';

export interface UpdateCategoryRequest {
  id: string;
  dto: UpdateCategoryDto;
}

export class UpdateCategoryUseCase implements IUpdateCategoryUseCase {
  constructor(
    @Inject(CATEGORY_REPOSITORY)
    private readonly categoryRepository: ICategoryRepository,
  ) {}

  public async execute(categoryData: UpdateCategoryRequest): Promise<Category> {
    const existingCategory =
      await this.categoryRepository.findByIdWithRelations(categoryData.id);
    if (!existingCategory) {
      throw new CategoryNotFoundException('id', categoryData.id);
    }

    return await this.categoryRepository.update(existingCategory);
  }
}
