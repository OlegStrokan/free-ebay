import { faker } from '@faker-js/faker';
import { Injectable } from '@nestjs/common';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';
import { OrderItemData } from '../order-item';
import { OrderItem } from '../order-item';
import { IOrderItemMockService } from './order-item-mock.interface';

@Injectable()
export class OrderItemMockService implements IOrderItemMockService {
  getOne(overrides: Partial<OrderItemData> = {}): OrderItem {
    const orderItemData: OrderItemData = {
      id: overrides.id ?? generateUlid(),
      productId: overrides.productId ?? generateUlid(),
      orderId: overrides.orderId ?? generateUlid(),
      quantity: overrides.quantity ?? faker.number.int(10),
      priceAtPurchase:
        overrides.priceAtPurchase ??
        new Money(faker.number.int(100), 'USD', 100),
      createdAt: overrides.createdAt ?? new Date(),
      updatedAt: overrides.updatedAt ?? new Date(),
    };

    return new OrderItem(orderItemData);
  }
  getMany(count = 2, overrides: Partial<OrderItemData>[] = []): OrderItem[] {
    return Array.from({ length: count }).map((_, index) =>
      this.getOne(overrides[index] as Partial<OrderItemData>),
    );
  }
}
