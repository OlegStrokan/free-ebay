import { Product } from 'src/product/core/product/entity/product';
import { ProductDb } from '../entity/product.entity';
import { Money } from 'src/shared/types/money';
import { ProductData } from 'src/product/core/product/entity/product.interface';

export class ProductMapper {
  static toDb(product: Product): ProductDb {
    const productData = product.data;

    const productDb = new ProductDb();
    productDb.id = product.id;
    productDb.name;
    productDb.sku = productData.sku;
    productDb.status = productData.status;
    productDb.price = productData.price
      ? MoneyMapper.toDb(productData.price)
      : null;
    productDb.discontinuedAt = productData.discontinuedAt;
    productDb.createdAt = productData.createdAt;
    productDb.updatedAt = productData.updatedAt;
    productDb.name = product.name;
    productDb.description = product.description;

    return productDb;
  }

  static toDomain(productDb: ProductDb): Product {
    const productData = {
      id: productDb.id,
      sku: productDb.sku,
      status: productDb.status,
      price: productDb.price ? (JSON.parse(productDb.price) as Money) : null,
      discontinuedAt: productDb.discontinuedAt,
      createdAt: productDb.createdAt,
      updatedAt: productDb.updatedAt,
      name: productDb.name,
      description: productDb.description,
    };

    return new Product(productData);
  }

  static toClient(product: Product): ProductData {
    return { ...product.data };
  }
}

export class MoneyMapper {
  static toDb(money: Money): string {
    return JSON.stringify(money);
  }

  static toDomain(moneyString: string | null): Money | null {
    return moneyString ? (JSON.parse(moneyString) as Money) : null;
  }
}
