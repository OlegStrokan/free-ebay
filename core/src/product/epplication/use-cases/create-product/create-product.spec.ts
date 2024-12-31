import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ICreateProductUseCase } from './create-product.interface';
import { TestingModule } from '@nestjs/testing';
import { CreateProductUseCase } from './create-product.use-case';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository';
import { clearRepos } from 'src/shared/testing-module/clear-repos';
import { createTestingModule } from 'src/shared/testing-module/test.module';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductMockService } from 'src/product/core/product/entity/mocks/product-mock.service';

describe('CreateProductUseCaseTest', () => {
  let createProductUseCase: ICreateProductUseCase;
  let productRepository: IProductRepository;
  let productMockService: IProductMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createProductUseCase =
      module.get<ICreateProductUseCase>(CreateProductUseCase);
    productRepository = module.get<IProductRepository>(ProductRepository);
    productMockService = module.get<IProductMockService>(ProductMockService);
  });

  beforeAll(async () => {
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
  });
});
