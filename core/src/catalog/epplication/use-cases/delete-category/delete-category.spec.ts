import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { ICategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.interface';
import { IDeleteCategoryUseCase } from './delete-category.interface';
import { CategoryNotFoundException } from 'src/catalog/core/category/entity/exceptions/category-not-found.exception';

describe('GeleteCategoryUseCaseTest', () => {
  let deleteCategoryUseCase: IDeleteCategoryUseCase;
  let categoryMockService: ICategoryMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    deleteCategoryUseCase = module.get(IDeleteCategoryUseCase);
    categoryMockService = module.get(ICategoryMockService);
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
