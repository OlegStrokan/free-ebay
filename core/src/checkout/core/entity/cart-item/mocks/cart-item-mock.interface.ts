import { AddToCartDto } from 'src/checkout/interface/dtos/add-to-cart.dto';
import { CartItemData, CartItem } from '../cart-item';

export interface ICartItemMockService {
  getOne(overrides?: Partial<CartItemData>): CartItem;
  getMany(count?: number, overrides?: Partial<CartItemData>[]): CartItem[];
  createOne(overrides?: Partial<CartItemData>): Promise<CartItem>;
  getOneToCreate(overrides?: Partial<AddToCartDto>): AddToCartDto;
}
