import { Order } from 'src/checkout/core/entity/order/order';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IGetAllUserOrdersUseCase = IUseCase<string, Order[]>;
