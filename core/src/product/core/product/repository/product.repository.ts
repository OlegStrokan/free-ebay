import { IClearableRepository } from 'src/shared/types/clearable';
import { Product } from '../entity/product';
import { ProductStatus } from '../entity/product-status';
import { ProductData } from '../entity/product.interface';

export abstract class IProductRepository extends IClearableRepository {
  abstract save(product: Product): Promise<Product>;
  abstract findById(id: string): Promise<Product | null>;
  abstract findBySku(sku: string): Promise<Product | null>;
  abstract findAll(page: number, limit: number): Promise<Product[]>;
  abstract deleteById(id: string): Promise<void>;
  abstract findByStatus(
    status: ProductStatus,
    page: number,
    limit: number,
  ): Promise<Product[]>;
  abstract update(product: Product): Promise<Product>;
  abstract discontinue(productData: ProductData): Promise<Product>;
  abstract findByAvailability(
    isAvailable: boolean,
    page: number,
    limit: number,
  ): Promise<Product[]>;
}
