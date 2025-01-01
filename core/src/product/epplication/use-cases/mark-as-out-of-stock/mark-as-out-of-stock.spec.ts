import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductMockService } from 'src/product/core/product/entity/mocks/product-mock.service';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { MarkAsOutOfStockUseCase } from './mark-as-out-of-stock.use-case';
import { IMarkAsOutOfStockUseCase } from './mark-as-out-of-stock.interface';

describe('MarkAsAvailableUseCaseTest', () => {
  let markAsOutOfStockUseCase: IMarkAsOutOfStockUseCase;
  let productMockService: IProductMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    markAsOutOfStockUseCase = module.get<IMarkAsOutOfStockUseCase>(
      MarkAsOutOfStockUseCase,
    );
    productMockService = module.get<IProductMockService>(ProductMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully mark product as out of stock', async () => {
    // Available it's default status for product
    const product = await productMockService.createOne();

    const retrievedProduct = await markAsOutOfStockUseCase.execute(product.id);

    expect(retrievedProduct).toBeDefined();
    expect(retrievedProduct.currentStatus).toBe(ProductStatus.OutOfStock);
  });
});
