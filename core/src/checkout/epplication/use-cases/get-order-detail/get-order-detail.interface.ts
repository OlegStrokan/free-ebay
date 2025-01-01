import { Order } from 'src/checkout/core/entity/order';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IGetOrderDetailsUseCase = IUseCase<string, Order>;
