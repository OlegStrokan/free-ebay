import { Cart } from 'src/checkout/core/entity/cart/cart';
import { AddToCartDto } from 'src/checkout/interface/dtos/add-to-cart.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IAddToCartUseCase = IUseCase<AddToCartDto, Cart>;
