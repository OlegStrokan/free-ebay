import { OrderItem, OrderItemData } from '../order-item';

export abstract class IOrderItemMockService {
  abstract getOne(overrides?: Partial<OrderItemData>): OrderItem;
  abstract getMany(
    count?: number,
    overrides?: Partial<OrderItemData>[],
  ): OrderItem[];
}
