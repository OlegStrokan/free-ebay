import { Inject, Injectable } from '@nestjs/common';
import { faker } from '@faker-js/faker';
import { Product } from 'src/product/core/product/entity/product';
import { Money } from 'src/shared/types/money';
import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { IProductMockService } from './product-mock.interface';
import { IProductRepository } from '../../repository/product.repository';
import { ProductData } from '../product.interface';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository';

@Injectable()
export class ProductMockService implements IProductMockService {
  constructor(
    @Inject(ProductRepository)
    private readonly productRepository: IProductRepository,
  ) {}

  getOneToCreate(): CreateProductDto {
    const randomPrice: Money = {
      amount: faker.number.int({ min: 1, max: 1000 }),
      currency: 'USD',
      fraction: 100,
    };
    const sku = faker.string.uuid();

    return {
      name: faker.commerce.productName(),
      description: faker.commerce.productDescription(),
      price: randomPrice,
      sku: sku,
    };
  }
  getOne(overrides: Partial<ProductData>): Product {
    const randomPrice: Money = {
      amount:
        overrides?.price?.amount ?? faker.number.int({ min: 1, max: 1000 }),
      currency: overrides?.price?.currency ?? 'USD',
      fraction: overrides?.price?.fraction ?? 100,
    };

    const sku = overrides?.sku ?? faker.string.uuid();

    const discontinuedAt =
      overrides?.discontinuedAt ?? faker.datatype.boolean()
        ? faker.date.past()
        : null;

    return Product.create({
      name: overrides?.name ?? faker.commerce.productName(),
      description:
        overrides?.description ?? faker.commerce.productDescription(),
      price: randomPrice,
      sku: sku,
      discontinuedAt: discontinuedAt ?? undefined,
    });
  }

  async createOne(overrides: Partial<ProductData>): Promise<Product> {
    const product = this.getOne(overrides);
    return await this.productRepository.save(product);
  }
}
