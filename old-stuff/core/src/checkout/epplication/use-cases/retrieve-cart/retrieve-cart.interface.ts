import { Cart } from 'src/checkout/core/entity/cart/cart';

export abstract class IRetrieveCartUseCase {
  abstract execute(cartId: string): Promise<Cart>;
}
