import { AddToCartDto } from 'src/checkout/interface/dtos/add-to-cart.dto';
import { CartItemData, CartItem } from '../cart-item';

export abstract class ICartItemMockService {
  abstract getOne(overrides?: Partial<CartItemData>): CartItem;
  abstract getMany(
    count?: number,
    overrides?: Partial<CartItemData>[],
  ): CartItem[];
  abstract getOneToCreate(overrides?: Partial<AddToCartDto>): AddToCartDto;
}
