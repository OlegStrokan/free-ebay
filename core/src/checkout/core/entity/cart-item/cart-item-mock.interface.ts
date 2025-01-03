import { AddToCartDto } from 'src/checkout/interface/dtos/add-to-cart.dto';
import { CartItem } from './cart-item';
import { CartItemData } from './cart-item';

export interface ICartItemMockService {
  getOne(overrides?: Partial<CartItemData>): CartItem;
  createOne(overrides?: Partial<CartItemData>): Promise<CartItem>;
  getOneToCreate(overrides?: Partial<AddToCartDto>): AddToCartDto;
}
