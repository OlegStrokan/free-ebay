import { Inject, Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import {
  Category,
  CategoryData,
} from 'src/catalog/core/category/entity/category';
import { CategoryNotFoundException } from 'src/catalog/core/category/entity/exceptions/category-not-found.exception';
import { Repository } from 'typeorm';
import { CategoryDb } from '../entity/category.entity';
import { ICategoryMapper } from '../mapper/category.mapper.interface';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { IClearableRepository } from 'src/shared/types/clearable';
import { CATEGORY_MAPPER } from 'src/catalog/epplication/injection-tokens/mapper.token';

@Injectable()
export class CategoryRepository
  implements ICategoryRepository, IClearableRepository
{
  constructor(
    @InjectRepository(CategoryDb)
    private readonly categoryRepository: Repository<CategoryDb>,
    @Inject(CATEGORY_MAPPER)
    private readonly mapper: ICategoryMapper<
      CategoryData,
      Category,
      CategoryDb
    >,
  ) {}

  async save(category: Category): Promise<Category> {
    const categoryDb = this.mapper.toDb(category);

    await this.categoryRepository.save(categoryDb);

    const savedCategory = await this.findByIdWithRelations(categoryDb.id);
    if (!savedCategory) {
      throw new CategoryNotFoundException('id', categoryDb.id);
    }
    return savedCategory;
  }

  async findById(id: string): Promise<Category | null> {
    const categoryDb = await this.categoryRepository.findOneBy({ id });
    return categoryDb ? this.mapper.toDomain(categoryDb) : null;
  }
  async findByIdWithRelations(id: string): Promise<Category | null> {
    const categoryDb = await this.categoryRepository.findOne({
      where: { id },
      relations: ['products'],
    });
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
    const updatedCategory = await this.categoryRepository.save(dbCategory);

    return this.mapper.toDomain(updatedCategory);
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
