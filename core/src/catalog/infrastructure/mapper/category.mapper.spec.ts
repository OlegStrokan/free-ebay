import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { ICategoryMapper } from './category.mapper.interface';
import {
  CategoryData,
  Category,
} from 'src/catalog/core/category/entity/category';
import { CategoryDb } from '../entity/category.entity';
import { ICategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.interface';
import { CategoryDto } from 'src/catalog/interface/dtos/category.dto';

const validateCategoryDataStructure = (
  categoryData: CategoryData | CategoryDto | undefined,
) => {
  if (!categoryData) throw new Error('Category not found test error');

  expect(categoryData).toEqual({
    id: expect.any(String),
    name: expect.any(String),
    description: expect.any(String),
    parentCategoryId: categoryData.parentCategoryId
      ? expect.any(String)
      : undefined,
    children: expect.any(Array),
    products: expect.any(Array),
  });
};

describe('CategoryMapperTest', () => {
  let module: TestingModule;
  let categoryMapper: ICategoryMapper;
  let categoryMockService: ICategoryMockService;

  beforeAll(async () => {
    module = await createTestingModule();

    categoryMapper = module.get(ICategoryMapper);

    categoryMockService = module.get(ICategoryMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully transform domain category to client (dto) category', async () => {
    const domainCategory = categoryMockService.getOne();
    const dtoCategory = categoryMapper.toClient(domainCategory);
    validateCategoryDataStructure(dtoCategory);
  });

  it('should successfully map database category to domain category', async () => {
    const dbCategory = new CategoryDb();
    dbCategory.id = '123';
    dbCategory.name = 'Category 1';
    dbCategory.description = 'Sample category description';
    dbCategory.parentCategory = undefined;
    dbCategory.children = [];
    dbCategory.products = [];

    const domainCategory = categoryMapper.toDomain(dbCategory);
    validateCategoryDataStructure(domainCategory.data);
  });
});
