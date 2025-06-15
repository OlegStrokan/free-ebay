import { Order } from 'src/checkout/core/entity/order/order';

export abstract class IGetOrderDetailsUseCase {
  abstract execute(orderId: string): Promise<Order>;
}
