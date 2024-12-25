import { Clonable } from 'src/shared/types/clonable';
import { ProductStatus } from './product-status';
import { Money, ZERO_AMOUNT_MONEY } from 'src/shared/types/money';
import { ProductData } from './product.interface';
import { InvalidProductStatusError } from '../error';
import { toISO8601UTC } from 'src/shared/types/dates';

export class Product implements Clonable<Product> {
  constructor(public product: ProductData) {}

  static create = (productData: Omit<ProductData, 'id' | 'status'>) =>
    new Product({
      ...productData,
      id: this.generateProductId(productData.sku, productData.createdAt),
      status: Product.getInitialStatus(),
    });

  static generateProductId = (
    sku: string,
    createdAt: ProductData['createdAt'],
  ) => `${sku.trim()}-${createdAt.trim()}`;

  static getInitialStatus = () => ProductStatus.Available;

  get id(): ProductData['id'] {
    return this.product.id;
  }

  get data(): ProductData {
    return this.product;
  }

  get currentStatus(): ProductStatus {
    return this.product.status;
  }

  get price(): Money {
    return this.product.price ?? ZERO_AMOUNT_MONEY;
  }

  isAvailable = () => {
    return this.product.status === ProductStatus.Available;
  };

  isOutOfStock = () => {
    return this.product.status === ProductStatus.OutOfStock;
  };

  markAsOutOfStock = () => {
    const clone = this.clone();
    clone.product.status = ProductStatus.OutOfStock;
    return clone;
  };

  markAsAvailable = () => {
    const clone = this.clone();
    clone.product.status = ProductStatus.Available;
    return clone;
  };

  updatePrice = (newPrice: Money) => {
    const clone = this.clone();
    clone.product.price = newPrice;
    return clone;
  };

  discontinue = () => {
    if (!this.isAvailable()) {
      throw new InvalidProductStatusError(
        'Cannot discontinue a product that is not available.',
        this.data,
      );
    }
    const clone = this.clone();
    clone.product.status = ProductStatus.Discontinued;
    clone.product.discontinuedAt = toISO8601UTC(new Date());
    return clone;
  };

  clone = (): Product => new Product({ ...this.product });
}
