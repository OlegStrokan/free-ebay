import { Injectable, NotImplementedException } from '@nestjs/common';
import { faker } from '@faker-js/faker';

import { ICartItemMockService } from './cart-item-mock.interface';
import { Money } from 'src/shared/types/money';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { CartItemData } from '../cart-item';
import { CartItem } from '../cart-item';
import { AddToCartDto } from 'src/checkout/interface/dtos/add-to-cart.dto';

@Injectable()
export class CartItemMockService implements ICartItemMockService {
  getOneToCreate(overrides: Partial<AddToCartDto> = {}): AddToCartDto {
    return {
      cartId: overrides?.cartId ?? generateUlid(),
      productId: overrides.productId ?? generateUlid(),
      quantity: overrides?.quantity ?? faker.number.int(10),
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

  getMany(count = 2, overrides: Partial<CartItemData>[] = []): CartItem[] {
    return Array.from({ length: count }).map((_, index) =>
      this.getOne(overrides[index] as Partial<CartItemData>),
    );
  }

  async createOne(overrides: Partial<CartItemData> = {}): Promise<CartItem> {
    throw new NotImplementedException();
  }
}
