import { Cart } from 'src/checkout/core/entity/cart/cart';
import { RemoveFromCartDto } from 'src/checkout/interface/dtos/remove-from-cart.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IRemoveFromCartUseCase = IUseCase<RemoveFromCartDto, Cart>;
