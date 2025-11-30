import { Order, OrderData } from 'src/checkout/core/entity/order/order';

export abstract class ICancelOrderUseCase {
  abstract execute(orderId: OrderData['id']): Promise<Order>;
}
