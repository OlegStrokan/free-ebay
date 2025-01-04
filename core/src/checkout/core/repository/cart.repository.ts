import { Cart } from 'src/checkout/core/entity/cart/cart';

export interface ICartRepository {
  saveCart(cart: Cart): Promise<Cart>;
  updateCart(cart: Cart): Promise<Cart>;
  getCartByUserId(userId: string): Promise<Cart | null>;
  getCartById(id: string): Promise<Cart | null>;
  getOneByIdIdWithRelations(userId: string): Promise<Cart | null>;
  getCartByUserIdWithRelations(userId: string): Promise<Cart | null>;
}
