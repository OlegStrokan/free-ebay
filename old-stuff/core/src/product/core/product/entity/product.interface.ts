import { Money } from 'src/shared/types/money';
import { ProductStatus } from './product-status';
import { Category } from 'src/catalog/core/category/entity/category';

export interface ProductData {
  id: string;
  sku: string;
  name: string;
  description: string;
  price: Money;
  status: ProductStatus;
  stock: number;
  createdAt: Date;
  updatedAt: Date;
  category?: Category;
  discontinuedAt?: Date;
}
