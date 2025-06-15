import { Cart } from 'src/checkout/core/entity/cart/cart';
import { AddToCartDto } from 'src/checkout/interface/dtos/add-to-cart.dto';

export abstract class IAddToCartUseCase {
  abstract execute(dto: AddToCartDto): Promise<Cart>;
}
