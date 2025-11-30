import { UserData } from 'src/user/core/entity/user';
import { Order, OrderData } from '../order';
import { CreateOrderDto } from 'src/checkout/interface/dtos/create-order.dto';
import { OrderItemData } from '../../order-item/order-item';

export abstract class IOrderMockService {
  abstract getOne(overrides?: Partial<OrderData>): Order;
  abstract createOne(overrides?: Partial<OrderData>): Promise<Order>;
  abstract createOneWithDependencies(
    orderOverrides?: Partial<OrderData>,
    userOverrides?: Partial<UserData>,
    orderItemsOverrides?: Partial<OrderItemData>[],
    countOfOrderItems?: number,
  ): Promise<Order>;
  abstract getOneToCreate(overrides?: Partial<CreateOrderDto>): CreateOrderDto;
}
