import { Injectable, NotImplementedException } from '@nestjs/common';
import { faker } from '@faker-js/faker';

import { ICartItemMockService } from './cart-item-mock.interface';
import { CartItem, CartItemData } from './cart-item';
import { Money } from 'src/shared/types/money';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { AddToCartDto } from 'src/checkout/interface/dtos/add-to-cart.dto';

@Injectable()
export class CartItemMockService implements ICartItemMockService {
  getOneToCreate(overrides: Partial<AddToCartDto> = {}): AddToCartDto {
    return {
      quantity: overrides?.quantity ?? 1,
      productId: overrides?.productId ?? generateUlid(),
      cartId: overrides?.cartId ?? generateUlid(),
    };
  }

  getOne(overrides: Partial<CartItemData> = {}): CartItem {
    const cartData: CartItemData = {
      id: overrides.id ?? generateUlid(),
      quantity: overrides.quantity ?? faker.number.int(10),
      productId: overrides.productId ?? generateUlid(),
      cartId: overrides.cartId ?? generateUlid(),
      price:
        overrides.price ??
        new Money(faker.number.int({ min: 10, max: 100 }), 'USD', 100),
      createdAt: overrides.createdAt ?? new Date(),
      updatedAt: overrides.updatedAt ?? new Date(),
    };

    return new CartItem(cartData);
  }

  async createOne(overrides: Partial<CartItemData> = {}): Promise<CartItem> {
    throw new NotImplementedException();
  }
}
