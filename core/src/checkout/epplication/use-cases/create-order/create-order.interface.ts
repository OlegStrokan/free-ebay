import { Order } from 'src/checkout/core/entity/order/order';
import { CreateOrderDto } from 'src/checkout/interface/dtos/create-order.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type ICreateOrderUseCase = IUseCase<CreateOrderDto, Order>;
