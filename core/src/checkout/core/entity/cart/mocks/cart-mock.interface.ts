import { CartData } from '../cart';
import { Cart } from '../cart';

export interface ICartMockService {
  getOne(overrides?: Partial<CartData>): Cart;
  createOne(overrides?: Partial<CartData>): Promise<Cart>;
  getOneToCreate(): Partial<CartData>;
}
