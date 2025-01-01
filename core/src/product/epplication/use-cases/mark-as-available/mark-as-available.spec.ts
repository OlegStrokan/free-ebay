import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductMockService } from 'src/product/core/product/entity/mocks/product-mock.service';
import { MarkAsAvailableUseCase } from './mark-as-available.use-case';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { IMarkAsAvailableUseCase } from './mark-as-available.interface';

describe('MarkAsAvailableUseCaseTest', () => {
  let markAsAvailableUseCase: IMarkAsAvailableUseCase;
  let productMockService: IProductMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    markAsAvailableUseCase = module.get<IMarkAsAvailableUseCase>(
      MarkAsAvailableUseCase,
    );
    productMockService = module.get<IProductMockService>(ProductMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully mark product as available', async () => {
    const product = await productMockService.createOne({
      status: ProductStatus.OutOfStock,
    });

    const retrievedProduct = await markAsAvailableUseCase.execute(product.id);

    expect(retrievedProduct).toBeDefined();
    expect(retrievedProduct.currentStatus).toBe(ProductStatus.Available);
  });
});
