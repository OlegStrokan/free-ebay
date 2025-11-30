import { Cart } from 'src/checkout/core/entity/cart/cart';

export abstract class IClearCartUseCase {
  abstract execute(cartId: string): Promise<Cart>;
}
