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
    productData: Omit<
      ProductData,
      'id' | 'status' | 'updatedAt' | 'createdAt' | 'stock'
    >,
  ) =>
    new Product({
      ...productData,
      id: generateUlid(),
      status: Product.getInitialStatus(),
      createdAt: new Date(),
      updatedAt: new Date(),
      stock: 0,
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
    const discountAmount = (clone.price.getAmount() * discountPercentage) / 100;
    const newAmount = clone.price.getAmount() - discountAmount;
    clone.product.price = new Money(
      newAmount,
      clone.price.getCurrency(),
      clone.price.getFraction(),
    );
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

  adjustPricingAndInventory = (salesData: {
    soldUnits: number;
    promotionActive: boolean;
  }) => {
    if (!this.isAvailable()) {
      throw new InvalidProductStatusException(
        'Cannot adjust pricing and inventory for a product that is not available.',
      );
    }

    const clone = this.clone();
    const { soldUnits, promotionActive } = salesData;

    clone.product.stock = Math.max(0, clone.product.stock - soldUnits);

    let newAmount = clone.price.getAmount();
    if (clone.product.stock < 10) {
      newAmount *= 1.1;
    } else if (promotionActive) {
      newAmount *= 0.9;
    }
    clone.product.price = new Money(
      newAmount,
      clone.price.getCurrency(),
      clone.price.getFraction(),
    );

    if (clone.product.stock === 0) {
      clone.product.status = ProductStatus.OutOfStock;
    }

    clone.product.updatedAt = new Date();

    return clone;
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
