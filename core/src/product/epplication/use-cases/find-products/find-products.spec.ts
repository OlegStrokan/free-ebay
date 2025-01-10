import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IFindProductsUseCase } from './find-product.interface';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { FIND_PRODUCTS_USE_CASE } from '../../injection-tokens/use-case.token';
import { PRODUCT_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';

describe('FindProductsUseCaseTest', () => {
  let findProductsUseCase: IFindProductsUseCase;
  let productMockService: IProductMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    findProductsUseCase = module.get<IFindProductsUseCase>(
      FIND_PRODUCTS_USE_CASE,
    );
    productMockService = module.get<IProductMockService>(PRODUCT_MOCK_SERVICE);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully retrieve products', async () => {
    await productMockService.createOne();
    await productMockService.createOne();
    const retrievedProducts = await findProductsUseCase.execute();

    expect(retrievedProducts).toBeDefined();
    expect(retrievedProducts).toHaveLength(2);
  });

  it('should succesfully retrieve empty array of products', async () => {
    const retrievedProducts = await findProductsUseCase.execute();

    expect(retrievedProducts).toBeDefined();
    expect(retrievedProducts).toHaveLength(0);
  });
});
