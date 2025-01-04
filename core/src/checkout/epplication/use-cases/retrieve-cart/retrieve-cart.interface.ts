import { Cart } from 'src/checkout/core/entity/cart/cart';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IRetrieveCartUseCase = IUseCase<string, Cart>;
