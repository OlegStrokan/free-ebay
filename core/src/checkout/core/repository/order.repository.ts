import { Order } from '../entity/order';

export interface IOrderRepository {
  createOrder(orderData: Partial<Order>): Promise<Order>;
  findById(orderId: string): Promise<Order>;
  cancelOrder(orderId: string): Promise<Order>;
  cancelOrder(orderId: string): Promise<Order>;
  findAll(): Promise<Order[]>;
}
