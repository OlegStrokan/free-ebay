import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';
import { OrderItemData } from '../order-item/order-item';
import { ShipmentData } from '../shipment/shipment';
import { PaymentData } from '../payment/payment';
import { InvalidOrderItemsException } from '../../exceptions/order/invalid-order-items.exception';

export enum OrderStatus {
  Pending = 'Pending',
  Shipped = 'Shipped',
  Cancelled = 'Cancelled',
}

export interface OrderData {
  id: string;
  userId: string;
  status: OrderStatus;
  items: OrderItemData[];
  totalPrice: Money;
  createdAt: Date;
  updatedAt: Date;
  shipment?: ShipmentData;
  payment?: PaymentData;
}

export class Order implements Clonable<Order> {
  constructor(public order: OrderData) {}

  static create = (
    orderData: Omit<
      OrderData,
      'id' | 'createdAt' | 'updatedAt' | 'status' | 'items'
    >,
  ) =>
    new Order({
      ...orderData,
      status: OrderStatus.Pending,
      id: generateUlid(),
      items: [],
      createdAt: new Date(),
      updatedAt: new Date(),
    });

  get id(): string {
    return this.order.id;
  }

  get data(): OrderData {
    return this.order;
  }

  get userId(): string {
    return this.order.userId;
  }

  get status(): OrderStatus {
    return this.order.status;
  }

  get items(): OrderItemData[] {
    return this.order.items;
  }

  get totalPrice(): Money {
    return this.order.totalPrice;
  }
  get shipment(): ShipmentData | undefined {
    return this.order.shipment;
  }

  get payment(): PaymentData | undefined {
    return this.order.payment;
  }
  markAsShipped = () => {
    const clone = this.clone();
    clone.order.status = OrderStatus.Shipped;
    return clone;
  };

  markAsCancelled = () => {
    const clone = this.clone();
    clone.order.status = OrderStatus.Cancelled;
    return clone;
  };

  applyDiscount = (discountPercentage: number) => {
    const clone = this.clone();
    const discountAmount =
      (clone.order.totalPrice.getAmount() * discountPercentage) / 100;
    const currentAmount = clone.order.totalPrice.getAmount();
    const newAmount = currentAmount - discountAmount;
    clone.order.totalPrice = new Money(
      newAmount,
      clone.order.totalPrice.getCurrency(),
      clone.order.totalPrice.getFraction(),
    );
    clone.order.updatedAt = new Date();
    return clone;
  };

  calculateTaxes = (taxRate: number) => {
    const clone = this.clone();
    const taxAmount = (clone.order.totalPrice.getAmount() * taxRate) / 100;
    const currentAmount = clone.order.totalPrice.getAmount();
    const newAmount = currentAmount + taxAmount;
    clone.order.totalPrice = new Money(
      newAmount,
      clone.order.totalPrice.getCurrency(),
      clone.order.totalPrice.getFraction(),
    );
    clone.order.updatedAt = new Date();
    return clone;
  };

  addItem = (item: OrderItemData) => {
    const clone = this.clone();
    if (item.quantity === 0) {
      throw new InvalidOrderItemsException();
    }
    clone.order.items.push(item);
    clone.order.totalPrice = this.calculateTotalPrice(clone.order.items);
    return clone;
  };

  addItems = (items: OrderItemData[]) => {
    const clone = this.clone();

    if (items.some((item) => item.quantity === 0)) {
      throw new InvalidOrderItemsException();
    }
    clone.order.items.push(...items);
    clone.order.totalPrice = this.calculateTotalPrice(clone.order.items);
    return clone;
  };

  removeItem = (orderItemId: string) => {
    const clone = this.clone();
    clone.order.items = clone.order.items.filter(
      (item) => item.id !== orderItemId,
    );
    clone.order.totalPrice = this.calculateTotalPrice(clone.order.items);
    return clone;
  };

  removeItems = (orderItemIds: string[]) => {
    const clone = this.clone();
    clone.order.items = clone.order.items.filter(
      (item) => !orderItemIds.includes(item.id),
    );
    clone.order.totalPrice = this.calculateTotalPrice(clone.order.items);
    return clone;
  };

  updateStatus = (status: OrderStatus) => {
    const clone = this.clone();
    clone.data.status = status;
    return clone;
  };

  private calculateTotalPrice(items: OrderItemData[]): Money {
    //@fix: fraction should be always 100;

    if (items.length === 0) {
      return Money.getDefaultMoney(0, 0);
    }

    const initialMoney = Money.getDefaultMoney(0, 0);

    return items.reduce(
      (total, item) => total.add(item.priceAtPurchase),
      initialMoney,
    );
  }

  clone = (): Order => new Order({ ...this.order });
}
