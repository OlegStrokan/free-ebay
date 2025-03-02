import { Inject } from '@nestjs/common';
import { Category } from 'src/catalog/core/category/entity/category';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { CreateCategoryDto } from 'src/catalog/interface/dtos/create-category.dto';
import { ICreateCategoryUseCase } from './create-category.interface';
import { CategoryAlreadyExistsException } from 'src/catalog/core/category/entity/exceptions/category-already-exists.exception';
import { CATEGORY_REPOSITORY } from '../../injection-tokens/repository.token';

export class CreateCategoryUseCase implements ICreateCategoryUseCase {
  constructor(
    @Inject(CATEGORY_REPOSITORY)
    private readonly categoryRepository: ICategoryRepository,
  ) {}

  public async execute(dto: CreateCategoryDto): Promise<Category> {
    const existingCategory = await this.categoryRepository.findByName(dto.name);
    if (existingCategory) {
      throw new CategoryAlreadyExistsException('name', existingCategory.name);
    }
    const category = Category.create({ ...dto, products: [] });
    await this.categoryRepository.save(category);
    return category;
  }
}
