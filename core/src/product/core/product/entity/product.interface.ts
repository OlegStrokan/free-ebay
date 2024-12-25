import { Money } from 'src/shared/types/money';
import { ProductStatus } from './product-status';

export interface ProductData {
  id: string;
  sku: string;
  name: string;
  description: string;
  price: Money;
  status: ProductStatus;
  createdAt: Date;
  updatedAt: Date;
  discontinuedAt?: Date;
}
