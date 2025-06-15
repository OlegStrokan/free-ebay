import { Category } from 'src/catalog/core/category/entity/category';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { IGetAllCategoriesUseCase } from './get-all-categories.interface';
import { Injectable } from '@nestjs/common';

@Injectable()
export class GetAllCategoriesUseCase implements IGetAllCategoriesUseCase {
  constructor(private readonly categoryRepository: ICategoryRepository) {}

  public async execute(): Promise<Category[]> {
    return this.categoryRepository.findAll(1, 100);
  }
}
