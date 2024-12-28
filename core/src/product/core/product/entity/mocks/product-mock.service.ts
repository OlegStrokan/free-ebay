import { Injectable } from '@nestjs/common';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository'; // Adjust the import path if necessary
import { faker } from '@faker-js/faker';
import { Product } from 'src/product/core/product/entity/product';
import { Money } from 'src/shared/types/money';
import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { IProductMockService } from './product-mock.interface';
import { IProductRepository } from '../../repository/product.repository';

@Injectable()
export class ProductMockService implements IProductMockService {
  constructor(private readonly productRepository: IProductRepository) {}

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
  getOne(overrides: Partial<Product>): Product {
    const randomPrice: Money = {
      amount:
        overrides.product.price.amount ??
        faker.number.int({ min: 1, max: 1000 }),
      currency: overrides.product.price.currency ?? 'USD',
      fraction: overrides.product.price.fraction ?? 100,
    };

    const sku = overrides.product.sku ?? faker.string.uuid();

    const discontinuedAt =
      overrides.product.discontinuedAt ?? faker.datatype.boolean()
        ? faker.date.past()
        : null;

    return Product.create({
      name: overrides.name ?? faker.commerce.productName(),
      description: overrides.description ?? faker.commerce.productDescription(),
      price: randomPrice,
      sku: sku,
      discontinuedAt: discontinuedAt,
    });
  }

  async createOne(overrides: Partial<Product>): Promise<Product> {
    const product = this.getOne(overrides);
    return await this.productRepository.save(product);
  }
}
