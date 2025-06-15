import { Category } from 'src/catalog/core/category/entity/category';
import { CategoryDb } from '../entity/category.entity';
import { CategoryDto } from 'src/catalog/interface/dtos/category.dto';

export abstract class ICategoryMapper {
  abstract toDb(domain: Category): CategoryDb;
  abstract toDomain(db: CategoryDb): Category;
  abstract toClient(domain: Category): CategoryDto;
}
