import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IFindProductsUseCase } from './find-product.interface';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';

describe('FindProductsUseCaseTest', () => {
  let findProductsUseCase: IFindProductsUseCase;
  let productMockService: IProductMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    findProductsUseCase = module.get(IFindProductsUseCase);
    productMockService = module.get(IProductMockService);
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
