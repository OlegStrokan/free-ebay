import { Inject, Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import {
  Category,
  CategoryData,
} from 'src/catalog/core/category/entity/category';
import { CategoryNotFoundException } from 'src/catalog/core/category/entity/exceptions/category-not-found.exception';
import { Repository } from 'typeorm';
import { CategoryDb } from '../entity/category';
import { CategoryMapper } from '../mapper/category.mapper';
import { ICategoryMapper } from '../mapper/category.mapper.interface';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { IClearableRepository } from 'src/shared/types/clearable';

@Injectable()
export class CategoryRepository
  implements ICategoryRepository, IClearableRepository
{
  constructor(
    @InjectRepository(CategoryDb)
    private readonly categoryRepository: Repository<CategoryDb>,
    @Inject(CategoryMapper)
    private readonly mapper: ICategoryMapper<
      CategoryData,
      Category,
      CategoryDb
    >,
  ) {}

  async save(category: Category): Promise<Category> {
    const categoryDb = this.mapper.toDb(category);

    await this.categoryRepository.save(categoryDb);

    const savedCategory = await this.findById(categoryDb.id);
    if (!savedCategory) {
      throw new CategoryNotFoundException('id', categoryDb.id);
    }
    return savedCategory;
  }

  async findById(id: string): Promise<Category | null> {
    const categoryDb = await this.categoryRepository.findOneBy({ id });
    return categoryDb ? this.mapper.toDomain(categoryDb) : null;
  }

  async findByName(name: string): Promise<Category | null> {
    const categoryDb = await this.categoryRepository.findOneBy({ name });
    return categoryDb ? this.mapper.toDomain(categoryDb) : null;
  }

  async findAll(page: number, limit: number): Promise<Category[]> {
    const [categoryDbs] = await this.categoryRepository.findAndCount({
      skip: (page - 1) * limit,
      take: limit,
    });
    return categoryDbs.map((categoryDb) => this.mapper.toDomain(categoryDb));
  }

  async update(category: Category): Promise<Category> {
    const dbCategory = this.mapper.toDb(category);
    const result = await this.categoryRepository.update(
      category.id,
      dbCategory,
    );
    if (result.affected === 0) {
      throw new CategoryNotFoundException('id', category.id);
    }
    return category;
  }

  async deleteById(id: string): Promise<void> {
    const result = await this.categoryRepository.delete(id);
    if (result.affected === 0) {
      throw new CategoryNotFoundException('id', id);
    }
  }

  async clear(): Promise<void> {
    await this.categoryRepository.query(`DELETE FROM "categories"`);
  }
}
