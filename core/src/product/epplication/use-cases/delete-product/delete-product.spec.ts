import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductMockService } from 'src/product/core/product/entity/mocks/product-mock.service';
import { DeleteProductUseCase } from './delete-product.use-case';
import { IDeleteProductUseCase } from './delete-product.interface';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';

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
    ).rejects.toThrow(ProductNotFoundException);
  });
});
