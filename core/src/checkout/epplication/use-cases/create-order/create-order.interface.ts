import { CreateOrderDto } from 'src/checkout/interface/dtos/create-order.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type ICreateOrderUseCase = IUseCase<CreateOrderDto, void>;
