import { Clonable } from 'src/shared/types/clonable';
import { ProductStatus } from './product-status';
import { Money, ZERO_AMOUNT_MONEY } from 'src/shared/types/money';
import { ProductData } from './product.interface';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { InvalidProductStatusException } from '../exceptions/invalid-product-status.exception';
import { RestockQuantityException } from '../exceptions/restock-quantity.exception';

export class Product implements Clonable<Product> {
  constructor(public product: ProductData) {}

  static create = (
    productData: Omit<ProductData, 'id' | 'status' | 'updatedAt' | 'createdAt'>,
  ) =>
    new Product({
      ...productData,
      id: generateUlid(),
      status: Product.getInitialStatus(),
      createdAt: new Date(),
      updatedAt: new Date(),
    });

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

  get name(): string {
    return this.product.name;
  }

  get description(): string {
    return this.product.description;
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
      throw new InvalidProductStatusException(
        'Cannot discontinue a product that is not available.',
      );
    }
    const clone = this.clone();
    clone.product.status = ProductStatus.Discontinued;
    clone.product.discontinuedAt = new Date();
    return clone;
  };

  applyDiscount = (discountPercentage: number) => {
    const clone = this.clone();
    const discountAmount = (clone.price.amount * discountPercentage) / 100;
    clone.product.price.amount -= discountAmount;
    return clone;
  };

  validateStatusTransition = (newStatus: ProductStatus) => {
    if (
      this.product.status === ProductStatus.Discontinued &&
      newStatus !== ProductStatus.Available
    ) {
      throw new InvalidProductStatusException(
        'Cannot change status from Discontinued to anything other than Available.',
      );
    }
  };

  restock = (quantity: number) => {
    if (quantity <= 0) {
      throw new RestockQuantityException();
    }
    const clone = this.clone();
    clone.product.stock += quantity;
    return clone;
  };

  clone = (): Product => new Product({ ...this.product });
}
