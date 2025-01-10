import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IGetAllCategoriesUseCase } from './get-all-categories.interface';
import { ICategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.interface';
import { CATEGORY_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';
import { GET_ALL_CATEGORIES_USE_CASE } from '../../injection-tokens/use-case.token';

describe('GetAllCategoriesTests', () => {
  let getAllCategoriesUseCase: IGetAllCategoriesUseCase;
  let categoryMockService: ICategoryMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getAllCategoriesUseCase = module.get<IGetAllCategoriesUseCase>(
      GET_ALL_CATEGORIES_USE_CASE,
    );
    categoryMockService = module.get<ICategoryMockService>(
      CATEGORY_MOCK_SERVICE,
    );
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully retrieve categories', async () => {
    await categoryMockService.createOne();
    await categoryMockService.createOne();
    const retrievedProducts = await getAllCategoriesUseCase.execute();

    expect(retrievedProducts).toBeDefined();
    expect(retrievedProducts).toHaveLength(2);
  });

  it('should succesfully retrieve empty array of categories', async () => {
    const retrievedProducts = await getAllCategoriesUseCase.execute();

    expect(retrievedProducts).toBeDefined();
    expect(retrievedProducts).toHaveLength(0);
  });
});
