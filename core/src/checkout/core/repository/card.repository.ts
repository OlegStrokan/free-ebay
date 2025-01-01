import { Cart } from '../entity/card';

export interface ICartRepository {
  addToCart(dto: any): Promise<Cart>;
  getCart(userId: string): Promise<Cart>;
  clearCart(userId: string): Promise<Cart>;
}
