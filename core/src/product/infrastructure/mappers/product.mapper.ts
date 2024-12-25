import { Product } from 'src/product/core/product/entity/product';
import { ProductDb } from '../entity/product.entity';

export class ProductMapper {
  static toDb(product: Product): ProductDb {
    const productData = product.data;

    const productDb = new ProductDb();
    productDb.id = product.id;
    productDb.name;
    productDb.sku = productData.sku;
    productDb.status = productData.status;
    productDb.price = productData.price;
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
      price: productDb.price,
      discontinuedAt: productDb.discontinuedAt,
      createdAt: productDb.createdAt,
      updatedAt: productDb.updatedAt,
      name: productDb.name,
      description: productDb.description,
    };

    return new Product(productData);
  }
}
