import { UserData } from 'src/user/core/entity/user';
import { Order, OrderData } from '../order';
import { CreateOrderDto } from 'src/checkout/interface/dtos/create-order.dto';
import { OrderItemData } from '../../order-item/order-item';

export interface IOrderMockService {
  getOne(overrides?: Partial<OrderData>): Order;
  createOne(overrides?: Partial<OrderData>): Promise<Order>;
  createOneWithDependencies(
    orderOverrides?: Partial<OrderData>,
    userOverrides?: Partial<UserData>,
    orderItemsOverrides?: Partial<OrderItemData>[],
    countOfOrderItems?: number,
  ): Promise<Order>;
  getOneToCreate(overrides?: Partial<CreateOrderDto>): CreateOrderDto;
}
