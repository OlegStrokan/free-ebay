import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing-module/test.module';
import { IProductMapper } from './product.mapper.interface';
import { ProductData } from 'src/product/core/product/entity/product.interface';
import { Product } from 'src/product/core/product/entity/product';
import { ProductDb } from '../../entity/product.entity';
import { ProductMapper } from './product.mapper';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductMockService } from 'src/product/core/product/entity/mocks/product-mock.service';
import { ProductStatus } from 'src/product/core/product/entity/product-status';

const validateProductDataStructure = (productData: ProductData | undefined) => {
  if (!productData) throw new Error('Product not found test error');

  expect(productData).toEqual({
    id: expect.any(String),
    sku: expect.any(String),
    status: expect.any(String),
    price: productData.price
      ? {
          amount: expect.any(Number),
          currency: expect.any(String),
          fraction: expect.any(Number),
        }
      : null,
    discontinuedAt: productData.discontinuedAt ? expect.any(Date) : null,
    createdAt: expect.any(Date),
    updatedAt: expect.any(Date),
    name: expect.any(String),
    description: expect.any(String),
  });
};

describe('ProductMapperTest', () => {
  let module: TestingModule;
  let productMapper: IProductMapper<ProductData, Product, ProductDb>;
  let productMockService: IProductMockService;

  beforeAll(async () => {
    module = await createTestingModule();

    productMapper =
      module.get<IProductMapper<ProductData, Product, ProductDb>>(
        ProductMapper,
      );

    productMockService = module.get<IProductMockService>(ProductMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully transform domain product to client (dto) product', async () => {
    const domainProduct = productMockService.getOne();
    const dtoProduct = productMapper.toClient(domainProduct);
    validateProductDataStructure(dtoProduct);
  });

  it('should successfully map database product to domain product', async () => {
    const dbProduct = new ProductDb();
    dbProduct.id = '123';
    dbProduct.sku = 'SKU001';
    dbProduct.status = ProductStatus.Available;
    dbProduct.price = JSON.stringify({
      amount: 1000,
      currency: 'USD',
      fraction: 2,
    });
    dbProduct.discontinuedAt = null;
    dbProduct.createdAt = new Date();
    dbProduct.updatedAt = new Date();
    dbProduct.name = 'Product 1';
    dbProduct.description = 'Sample product description';

    const domainProduct = productMapper.toDomain(dbProduct);
    validateProductDataStructure(domainProduct.data);
  });
});