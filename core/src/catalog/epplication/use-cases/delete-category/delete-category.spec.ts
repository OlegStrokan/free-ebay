import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { ICategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.interface';
import { IDeleteCategoryUseCase } from './delete-category.interface';
import { CategoryNotFoundException } from 'src/catalog/core/category/entity/exceptions/category-not-found.exception';
import { CATEGORY_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';
import { DELETE_CATEGORY_USE_CASE } from '../../injection-tokens/use-case.token';

describe('GeleteCategoryUseCaseTest', () => {
  let deleteCategoryUseCase: IDeleteCategoryUseCase;
  let categoryMockService: ICategoryMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    deleteCategoryUseCase = module.get<IDeleteCategoryUseCase>(
      DELETE_CATEGORY_USE_CASE,
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

  it('should succesfully delete category', async () => {
    const product = await categoryMockService.createOne();

    const retrievedProduct = await deleteCategoryUseCase.execute(product.id);

    expect(retrievedProduct).not.toBeDefined();
  });

  it('should succesfully delete category', async () => {
    await expect(
      deleteCategoryUseCase.execute('not_existing_id'),
    ).rejects.toThrow(CategoryNotFoundException);
  });
});
