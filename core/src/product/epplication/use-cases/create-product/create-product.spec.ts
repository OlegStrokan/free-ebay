import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ICreateProductUseCase } from './create-product.interface';
import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductAlreadyExistsException } from 'src/product/core/product/exceptions/product-already-exists.exception';
import { CREATE_PRODUCT_USE_CASE } from '../../injection-tokens/use-case.token';
import { PRODUCT_REPOSITORY } from '../../injection-tokens/repository.token';
import { PRODUCT_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';

describe('CreateProductUseCaseTest', () => {
  let createProductUseCase: ICreateProductUseCase;
  let productRepository: IProductRepository;
  let productMockService: IProductMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createProductUseCase = module.get<ICreateProductUseCase>(
      CREATE_PRODUCT_USE_CASE,
    );
    productRepository = module.get<IProductRepository>(PRODUCT_REPOSITORY);
    productMockService = module.get<IProductMockService>(PRODUCT_MOCK_SERVICE);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should create a random product and verify its existence', async () => {
    const productDto = productMockService.getOneToCreate();

    await createProductUseCase.execute(productDto);

    const retrievedProduct = await productRepository.findBySku(productDto.sku);

    expect(retrievedProduct).toBeDefined();
    expect(retrievedProduct?.data.name).toBe(productDto.name);
  });

  it('should throw error if product already exists', async () => {
    const productDto = productMockService.getOneToCreate();
    await productMockService.createOne({ sku: productDto.sku });

    await expect(createProductUseCase.execute(productDto)).rejects.toThrow(
      ProductAlreadyExistsException,
    );
  });
});
