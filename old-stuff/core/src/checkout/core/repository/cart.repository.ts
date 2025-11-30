import { Cart } from 'src/checkout/core/entity/cart/cart';

export abstract class ICartRepository {
  abstract saveCart(cart: Cart): Promise<Cart>;
  abstract updateCart(cart: Cart): Promise<Cart>;
  abstract getCartByUserId(userId: string): Promise<Cart | null>;
  abstract getCartById(id: string): Promise<Cart | null>;
  abstract getOneByIdIdWithRelations(userId: string): Promise<Cart | null>;
  abstract getCartByUserIdWithRelations(userId: string): Promise<Cart | null>;
}
