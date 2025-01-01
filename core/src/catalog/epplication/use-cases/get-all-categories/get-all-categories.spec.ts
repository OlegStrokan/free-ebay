import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IGetAllCategoriesUseCase } from './get-all-categories.interface';
import { ICategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.interface';
import { CategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.service';
import { GetAllCategoriesUseCase } from './get-all-categories.use-case';

describe('GetAllCategoriesTests', () => {
  let getAllCategoriesUseCase: IGetAllCategoriesUseCase;
  let categoryMockService: ICategoryMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getAllCategoriesUseCase = module.get<IGetAllCategoriesUseCase>(
      GetAllCategoriesUseCase,
    );
    categoryMockService = module.get<ICategoryMockService>(CategoryMockService);
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
