import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductMockService } from 'src/product/core/product/entity/mocks/product-mock.service';
import { IFindProductUseCase } from './find-product.interface';
import { FindProductUseCase } from './find-product.use-case';
import { faker } from '@faker-js/faker';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';

describe('FindProductUseCaseTest', () => {
  let findProductUseCase: IFindProductUseCase;
  let productMockService: IProductMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    findProductUseCase = module.get<IFindProductUseCase>(FindProductUseCase);
    productMockService = module.get<IProductMockService>(ProductMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully retrieve product', async () => {
    const productName = faker.commerce.product.name;
    await productMockService.createOne({ name: productName });
    const retrievedProduct = await findProductUseCase.execute();

    expect(retrievedProduct).toBeDefined();
    expect(retrievedProduct.name).toBe(productName);
  });
  it("should throw error if product dosn't exist", async () => {
    await expect(findProductUseCase.execute()).rejects.toThrow(
      ProductNotFoundException,
    );
  });
});
