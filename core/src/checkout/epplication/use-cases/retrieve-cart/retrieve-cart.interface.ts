import { Cart } from 'src/checkout/core/entity/card';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IRetrieveCartUseCase = IUseCase<null, Cart>;
