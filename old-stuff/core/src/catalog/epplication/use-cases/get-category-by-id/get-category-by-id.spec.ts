import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { faker } from '@faker-js/faker';
import { IGetCategoryByIdUseCase } from './get-category-by-id.interface';
import { CategoryNotFoundException } from 'src/catalog/core/category/entity/exceptions/category-not-found.exception';
import { ICategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.interface';

describe('GetCategoryByIdTest', () => {
  let getCategoryById: IGetCategoryByIdUseCase;
  let categoryMockService: ICategoryMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getCategoryById = module.get(IGetCategoryByIdUseCase);
    categoryMockService = module.get(ICategoryMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully retrieve category', async () => {
    const categoryName = faker.commerce.department();
    const category = await categoryMockService.createOne({
      name: categoryName,
    });
    const retrievedCategory = await getCategoryById.execute(category.id);

    expect(retrievedCategory).toBeDefined();
    expect(retrievedCategory.name).toBe(categoryName);
  });
  it("should throw error if category dosn't exist", async () => {
    await expect(getCategoryById.execute('not_existing_id')).rejects.toThrow(
      CategoryNotFoundException,
    );
  });
});
