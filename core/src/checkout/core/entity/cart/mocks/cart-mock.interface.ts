import { CartData } from '../cart';
import { Cart } from '../cart';

export abstract class ICartMockService {
  abstract getOne(overrides?: Partial<CartData>): Cart;
  abstract createOne(overrides?: Partial<CartData>): Promise<Cart>;
  abstract getOneToCreate(): Partial<CartData>;
}
