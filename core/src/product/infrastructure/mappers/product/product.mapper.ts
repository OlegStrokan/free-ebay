import { forwardRef, Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { ProductData } from 'src/product/core/product/entity/product.interface';
import { ProductDb } from '../../entity/product.entity';
import { IMoneyMapper } from '../money/money.mapper.interface';
import { IProductMapper } from './product.mapper.interface';
import { Money } from 'src/shared/types/money';
import { ICategoryMapper } from 'src/catalog/infrastructure/mapper/category.mapper.interface';
import { ProductDto } from 'src/product/interface/dtos/product.dto';

@Injectable()
export class ProductMapper implements IProductMapper {
  constructor(
    private readonly moneyMapper: IMoneyMapper,
    @Inject(forwardRef(() => ICategoryMapper))
    private readonly categoryMapper: ICategoryMapper,
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

  toClient(product: Product): ProductDto {
    const { data } = product;
    return {
      id: data.id,
      sku: data.sku,
      name: data.name,
      description: data.description,
      price: this.moneyMapper.toClient(data.price),
      status: data.status,
      stock: data.stock,
      createdAt: data.createdAt,
      updatedAt: data.updatedAt,
      category: data.category
        ? this.categoryMapper.toClient(data.category)
        : undefined,
      discontinuedAt: data.discontinuedAt,
    };
  }
}
