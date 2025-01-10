import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { ProductData } from 'src/product/core/product/entity/product.interface';
import { ProductDb } from '../../entity/product.entity';
import { IMoneyMapper } from '../money/money.mapper.interface';
import { IProductMapper } from './product.mapper.interface';
import { Money } from 'src/shared/types/money';
import { MONEY_MAPPER } from 'src/product/epplication/injection-tokens/mapper.token';

@Injectable()
export class ProductMapper
  implements IProductMapper<ProductData, Product, ProductDb>
{
  constructor(
    @Inject(MONEY_MAPPER)
    private readonly moneyMapper: IMoneyMapper,
  ) {}

  toDb(product: Product): ProductDb {
    const productData = product.data;
    const productDb = new ProductDb();

    productDb.id = product.id;
    productDb.sku = productData.sku;
    productDb.status = productData.status;
    productDb.price = productData.price
      ? this.moneyMapper.toDb(productData.price)
      : '';
    productDb.discontinuedAt = productData.discontinuedAt;
    productDb.createdAt = productData.createdAt;
    productDb.updatedAt = productData.updatedAt;
    productDb.name = product.name;
    productDb.description = product.description;
    productDb.stock = productData.stock;

    return productDb;
  }

  toDomain(productDb: ProductDb): Product {
    const productData: ProductData = {
      id: productDb.id,
      sku: productDb.sku,
      status: productDb.status,
      price:
        this.moneyMapper.toDomain(productDb.price) || Money.getDefaultMoney(),
      discontinuedAt: productDb.discontinuedAt,
      createdAt: productDb.createdAt,
      updatedAt: productDb.updatedAt,
      name: productDb.name,
      description: productDb.description,
      stock: productDb.stock ?? 0,
    };

    return new Product(productData);
  }

  toClient(product: Product): ProductData {
    return product.data;
  }
}
