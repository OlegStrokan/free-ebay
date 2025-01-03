import { IClearableRepository } from 'src/shared/types/clearable';
import { Product } from '../entity/product';
import { ProductStatus } from '../entity/product-status';
import { ProductData } from '../entity/product.interface';

export interface IProductRepository extends IClearableRepository {
  // Create or update a product
  save(product: Product): Promise<Product>;
  // Find a product by its unique ID
  findById(id: string): Promise<Product | null>;
  // Find a product by its SKU
  findBySku(sku: string): Promise<Product | null>;
  // Get all products (optional, can be paginated for large datasets)
  findAll(page: number, limit: number): Promise<Product[]>;
  // Delete a product by its unique ID
  deleteById(id: string): Promise<void>;
  // Find products by their status
  findByStatus(
    status: ProductStatus,
    page: number,
    limit: number,
  ): Promise<Product[]>;
  // Update the price of a product
  update(product: Product): Promise<Product>;
  // Mark a product as discontinued
  discontinue(productData: ProductData): Promise<Product>;
  // Find products based on their availability (Available, OutOfStock, etc.)
  findByAvailability(
    isAvailable: boolean,
    page: number,
    limit: number,
  ): Promise<Product[]>;
}
