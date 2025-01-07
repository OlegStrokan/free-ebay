import { Order } from '../entity/order/order';

export interface IOrderRepository {
  save(orderData: Order): Promise<Order>;
  update(cart: Order): Promise<Order>;
  findById(orderId: string): Promise<Order | null>;
  findByIdWithRelations(orderId: string): Promise<Order | null>;
  findAll(): Promise<Order[]>;
  findAllByUserId(userId: string): Promise<Order[]>;
}
