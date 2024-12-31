import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing-module/clear-repos';
import { createTestingModule } from 'src/shared/testing-module/test.module';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductMockService } from 'src/product/core/product/entity/mocks/product-mock.service';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { DeleteProductUseCase } from './delete-product.use-case';
import { IDeleteProductUseCase } from './delete-product.interface';
import { ProductNotFoundError } from 'src/product/core/product/error';

describe('MarkAsAvailableUseCaseTest', () => {
  let deleteProductUseCase: IDeleteProductUseCase;
  let productMockService: IProductMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    deleteProductUseCase =
      module.get<IDeleteProductUseCase>(DeleteProductUseCase);
    productMockService = module.get<IProductMockService>(ProductMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully delete product', async () => {
    const product = await productMockService.createOne();

    const retrievedProduct = await deleteProductUseCase.execute(product.id);

    expect(retrievedProduct).not.toBeDefined();
  });

  it('should succesfully delete product', async () => {
    await expect(
      deleteProductUseCase.execute('not_existing_id'),
    ).rejects.toThrow(ProductNotFoundError);
  });
});
