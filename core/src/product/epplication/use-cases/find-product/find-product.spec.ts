import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { IFindProductUseCase } from './find-product.interface';
import { faker } from '@faker-js/faker';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';
import { FIND_PRODUCT_USE_CASE } from '../../injection-tokens/use-case.token';
import { PRODUCT_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';

describe('FindProductUseCaseTest', () => {
  let findProductUseCase: IFindProductUseCase;
  let productMockService: IProductMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    findProductUseCase = module.get<IFindProductUseCase>(FIND_PRODUCT_USE_CASE);
    productMockService = module.get<IProductMockService>(PRODUCT_MOCK_SERVICE);
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
