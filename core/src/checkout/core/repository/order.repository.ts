import { Order } from '../entity/order/order';

export abstract class IOrderRepository {
  abstract save(orderData: Order): Promise<Order>;
  abstract update(cart: Order): Promise<Order>;
  abstract findById(orderId: string): Promise<Order | null>;
  abstract findByIdWithRelations(orderId: string): Promise<Order | null>;
  abstract findAll(): Promise<Order[]>;
  abstract findAllByUserId(userId: string): Promise<Order[]>;
}
