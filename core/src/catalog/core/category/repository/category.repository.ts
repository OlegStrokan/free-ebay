import { Category } from '../entity/category';

export abstract class ICategoryRepository {
  abstract save(category: Category): Promise<Category>;
  abstract findById(id: string): Promise<Category | null>;
  abstract findByIdWithRelations(id: string): Promise<Category | null>;
  abstract findByName(name: string): Promise<Category | null>;
  abstract findAll(page: number, limit: number): Promise<Category[]>;
  abstract update(category: Category): Promise<Category>;
  abstract deleteById(id: string): Promise<void>;
  abstract clear(): Promise<void>;
}
