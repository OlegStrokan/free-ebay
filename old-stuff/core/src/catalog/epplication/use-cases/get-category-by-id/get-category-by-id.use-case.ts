import { Category } from 'src/catalog/core/category/entity/category';
import { CategoryNotFoundException } from 'src/catalog/core/category/entity/exceptions/category-not-found.exception';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { IGetCategoryByIdUseCase } from './get-category-by-id.interface';
import { Injectable } from '@nestjs/common';

@Injectable()
export class GetCategoryByIdUseCase implements IGetCategoryByIdUseCase {
  constructor(private readonly categoryRepository: ICategoryRepository) {}

  public async execute(id: string): Promise<Category> {
    const category = await this.categoryRepository.findById(id);
    if (!category) {
      throw new CategoryNotFoundException('id', id);
    }
    return category;
  }
}
