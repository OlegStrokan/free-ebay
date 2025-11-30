import { Order } from 'src/checkout/core/entity/order/order';

export abstract class IShipOrderUseCase {
  abstract execute(orderId: string): Promise<Order>;
}
