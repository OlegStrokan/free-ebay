import { ProductData } from './entity/product.interface';

export class InvalidProductStatusError extends Error {
  constructor(message: string, public productData: ProductData) {
    super(message);
    this.name = 'InvalidProductStatusError';
  }
}
