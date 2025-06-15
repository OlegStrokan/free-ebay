import { Order } from 'src/checkout/core/entity/order/order';

export abstract class IGetAllUserOrdersUseCase {
  abstract execute(userId: string): Promise<Order[]>;
}
