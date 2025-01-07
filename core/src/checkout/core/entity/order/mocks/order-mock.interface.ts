import { UserData } from 'src/user/core/entity/user';
import { Order, OrderData } from '../order';
import { CreateOrderDto } from 'src/checkout/interface/dtos/create-order.dto';

export interface IOrderMockService {
  getOne(overrides?: Partial<OrderData>): Order;
  createOne(overrides?: Partial<OrderData>): Promise<Order>;
  createOneWithDependencies(
    orderOverrides?: Partial<OrderData>,
    userOverrides?: Partial<UserData>,
    countOfOrderItems?: number,
  ): Promise<Order>;
  getOneToCreate(overrides?: Partial<CreateOrderDto>): CreateOrderDto;
}
