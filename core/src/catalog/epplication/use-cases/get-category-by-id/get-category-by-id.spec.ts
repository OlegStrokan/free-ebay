import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { faker } from '@faker-js/faker';
import { IGetCategoryByIdUseCase } from './get-category-by-id.interface';
import { CategoryNotFoundException } from 'src/catalog/core/category/entity/exceptions/category-not-found.exception';
import { CategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.service';
import { ICategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.interface';
import { GetCategoryByIdUseCase } from './get-category-by-id.use-case';

describe('GetCategoryByIdTest', () => {
  let getCategoryById: IGetCategoryByIdUseCase;
  let categoryMockService: ICategoryMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getCategoryById = module.get<IGetCategoryByIdUseCase>(
      GetCategoryByIdUseCase,
    );
    categoryMockService = module.get<ICategoryMockService>(CategoryMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully retrieve category', async () => {
    const categoryName = faker.commerce.department();
    await categoryMockService.createOne({ name: categoryName });
    const retrievedProduct = await getCategoryById.execute();

    expect(retrievedProduct).toBeDefined();
    expect(retrievedProduct.name).toBe(categoryName);
  });
  it("should throw error if category dosn't exist", async () => {
    await expect(getCategoryById.execute()).rejects.toThrow(
      CategoryNotFoundException,
    );
  });
});
