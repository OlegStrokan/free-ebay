import { Inject, Injectable } from '@nestjs/common';
import { CartData } from '../cart';
import { Cart } from '../cart';
import { Money } from 'src/shared/types/money';
import { ICartMockService } from './cart-mock.interface';
import { CART_REPOSITORY } from 'src/checkout/epplication/injection-tokens/repository.token';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import { generateUlid } from 'src/shared/types/generate-ulid';

@Injectable()
export class CartMockService implements ICartMockService {
  constructor(
    @Inject(CART_REPOSITORY)
    private readonly cartRepository: ICartRepository,
  ) {}

  getOneToCreate(): Partial<CartData> {
    return {
      userId: generateUlid(),
    };
  }

  getOne(overrides: Partial<CartData> = {}): Cart {
    const cartData: CartData = {
      id: overrides.id ?? generateUlid(),
      userId: overrides.userId ?? generateUlid(),
      items: overrides.items ?? [],
      totalPrice: new Money(100, 'USD', 100),
      createdAt: overrides.createdAt ?? new Date(),
      updatedAt: overrides.updatedAt ?? new Date(),
    };

    return new Cart(cartData);
  }

  async createOne(overrides: Partial<CartData> = {}): Promise<Cart> {
    const cart = this.getOne(overrides);
    return await this.cartRepository.saveCart(cart);
  }
}
