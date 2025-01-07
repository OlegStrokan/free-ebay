import { OrderItem, OrderItemData } from '../order-item';

export interface IOrderItemMockService {
  getOne(overrides?: Partial<OrderItemData>): OrderItem;
  getMany(count?: number, overrides?: Partial<OrderItemData>[]): OrderItem[];
}
