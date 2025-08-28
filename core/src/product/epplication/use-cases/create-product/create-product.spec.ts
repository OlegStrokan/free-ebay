import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ICreateProductUseCase } from './create-product.interface';
import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductAlreadyExistsException } from 'src/product/core/product/exceptions/product-already-exists.exception';
import { IKafkaProducerService } from 'src/shared/kafka/kafka-producer.interface';

describe('CreateProductUseCaseTest', () => {
  let createProductUseCase: ICreateProductUseCase;
  let productRepository: IProductRepository;
  let productMockService: IProductMockService;
  let kafkaProducerService: IKafkaProducerService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createProductUseCase = module.get(ICreateProductUseCase);
    productRepository = module.get(IProductRepository);
    productMockService = module.get(IProductMockService);
    kafkaProducerService = module.get(IKafkaProducerService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should create a random product and verify its existence', async () => {
    const productDto = productMockService.getOneToCreate();

    // Mock Kafka sendMessage
    const kafkaSpy = jest
      .spyOn(kafkaProducerService, 'sendMessage')
      .mockResolvedValue(undefined);

    await createProductUseCase.execute(productDto);

    const retrievedProduct = await productRepository.findBySku(productDto.sku);

    expect(retrievedProduct).toBeDefined();
    expect(retrievedProduct?.data.name).toBe(productDto.name);
    expect(retrievedProduct?.data.price).toStrictEqual(productDto.price);
    expect(kafkaSpy).toHaveBeenCalled();
    kafkaSpy.mockRestore();
  });

  it('should throw error if product already exists', async () => {
    const productDto = productMockService.getOneToCreate();
    await productMockService.createOne({ sku: productDto.sku });

    await expect(createProductUseCase.execute(productDto)).rejects.toThrow(
      ProductAlreadyExistsException,
    );
  });
});
