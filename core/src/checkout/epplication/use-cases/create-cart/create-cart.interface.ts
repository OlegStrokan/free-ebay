import { Cart } from 'src/checkout/core/entity/card';
import { CreateCartDto } from 'src/checkout/interface/dtos/create-cart.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type ICreateCartUseCase = IUseCase<CreateCartDto, Cart>;
