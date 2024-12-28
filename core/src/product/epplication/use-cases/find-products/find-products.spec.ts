import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing-module/clear-repos';
import { createTestingModule } from 'src/shared/testing-module/test.module';
import { IFindProductsUseCase } from './find-product.interface';
import { FindProductsUseCase } from './find-products.use-case';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductMockService } from 'src/product/core/product/entity/mocks/product-mock.service';

describe('FindProductsUseCaseTest', () => {
  let findProductsUseCase: IFindProductsUseCase;
  let productMockService: IProductMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    findProductsUseCase = module.get<IFindProductsUseCase>(FindProductsUseCase);
    productMockService = module.get<IProductMockService>(ProductMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully retrieve product', async () => {
    await productMockService.createOne();
    const retrievedProducts = await findProductsUseCase.execute();

    expect(retrievedProducts).toBeDefined();
    expect(retrievedProducts).toHaveLength(1);
  });
});