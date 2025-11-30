import { Order } from 'src/checkout/core/entity/order/order';
import { CreateOrderDto } from 'src/checkout/interface/dtos/create-order.dto';

export abstract class ICreateOrderUseCase {
  abstract execute(dto: CreateOrderDto): Promise<Order>;
}
