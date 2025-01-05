import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';

export interface OrderItemData {
  id: string;
  productId: string;
  orderId: string;
  quantity: number;
  priceAtPurchase: Money;
  createdAt: Date;
  updatedAt: Date;
}

export class OrderItem implements Clonable<OrderItem> {
  constructor(public item: OrderItemData) {}

  static create(
    itemData: Omit<OrderItemData, 'id' | 'createdAt' | 'updatedAt'>,
  ) {
    return new OrderItem({
      ...itemData,
      id: generateUlid(),
      createdAt: new Date(),
      updatedAt: new Date(),
    });
  }

  get id(): string {
    return this.item.id;
  }

  get data(): OrderItemData {
    return this.item;
  }

  get productId(): string {
    return this.item.productId;
  }

  get orderId(): string {
    return this.item.orderId;
  }

  get quantity(): number {
    return this.item.quantity;
  }

  get priceAtPurchase(): Money {
    return this.item.priceAtPurchase;
  }

  updateQuantity = (newQuantity: number) => {
    const clone = this.clone();
    clone.item.quantity = newQuantity;
    return clone;
  };

  updatePrice = (newPrice: Money) => {
    const clone = this.clone();
    clone.item.priceAtPurchase = newPrice;
    return clone;
  };

  clone = (): OrderItem => new OrderItem({ ...this.item });
}
