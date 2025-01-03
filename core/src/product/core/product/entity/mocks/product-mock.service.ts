import { Inject, Injectable } from '@nestjs/common';
import { faker } from '@faker-js/faker';
import { Product } from 'src/product/core/product/entity/product';
import { Money } from 'src/shared/types/money';
import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { IProductMockService } from './product-mock.interface';
import { IProductRepository } from '../../repository/product.repository';
import { ProductData } from '../product.interface';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository';
import { generateUlid } from 'src/shared/types/generate-ulid';

// ... existing code ...

@Injectable()
export class ProductMockService implements IProductMockService {
  constructor(
    @Inject(ProductRepository)
    private readonly productRepository: IProductRepository,
  ) {}

  private getRandomMoney(): Money {
    const amount = faker.number.int({ min: 1, max: 1000 });
    const currency = 'USD';
    const fraction = 100;
    return new Money(amount, currency, fraction);
  }

  getOneToCreate(): CreateProductDto {
    const randomPrice = this.getRandomMoney();
    const sku = generateUlid();

    return {
      name: faker.commerce.productName(),
      description: faker.commerce.productDescription(),
      price: randomPrice,
      sku: sku,
    };
  }

  getOne(overrides: Partial<ProductData>): Product {
    const randomPrice = overrides?.price ?? this.getRandomMoney();
    const sku = overrides?.sku ?? generateUlid();

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
