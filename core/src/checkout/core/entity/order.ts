import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';
import { CartItemData } from './order-item';

export enum OrderStatus {
  SHIPPED = 'Shipped',
  CANCELLED = 'Cancelled',
}

export interface OrderData {
  id: string;
  userId: string;
  status: string;
  items: CartItemData[];
  totalPrice: Money;
  createdAt: Date;
  updatedAt: Date;
}

export class Order implements Clonable<Order> {
  constructor(public order: OrderData) {}

  static create = (
    orderData: Omit<OrderData, 'id' | 'createdAt' | 'updatedAt'>,
  ) =>
    new Order({
      ...orderData,
      id: generateUlid(),
      createdAt: new Date(),
      updatedAt: new Date(),
    });

  get id(): string {
    return this.order.id;
  }

  get userId(): string {
    return this.order.userId;
  }

  get status(): string {
    return this.order.status;
  }

  get items(): CartItemData[] {
    return this.order.items;
  }

  get totalPrice(): Money {
    return this.order.totalPrice;
  }

  markAsShipped = () => {
    const clone = this.clone();
    clone.order.status = OrderStatus.SHIPPED;
    return clone;
  };

  markAsCancelled = () => {
    const clone = this.clone();
    clone.order.status = OrderStatus.CANCELLED;
    return clone;
  };

  applyDiscount = (discountPercentage: number) => {
    const clone = this.clone();
    const discountAmount =
      (clone.order.totalPrice.amount * discountPercentage) / 100;
    clone.order.totalPrice.amount -= discountAmount;
    clone.order.updatedAt = new Date();
    return clone;
  };

  calculateTaxes = (taxRate: number) => {
    const clone = this.clone();
    const taxAmount = (clone.order.totalPrice.amount * taxRate) / 100;
    clone.order.totalPrice.amount += taxAmount;
    clone.order.updatedAt = new Date();
    return clone;
  };

  clone = (): Order => new Order({ ...this.order });
}
