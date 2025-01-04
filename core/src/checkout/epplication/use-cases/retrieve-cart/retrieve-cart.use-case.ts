import { Inject, Injectable } from '@nestjs/common';
import { IRetrieveCartUseCase } from './retrieve-cart.interface';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import { CART_REPOSITORY } from '../../injection-tokens/repository.token';
import { Cart } from 'src/checkout/core/entity/cart/cart';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';

@Injectable()
export class RetrieveCartUseCase implements IRetrieveCartUseCase {
  constructor(
    @Inject(CART_REPOSITORY)
    private readonly cartRepository: ICartRepository,
  ) {}

  async execute(userId: string): Promise<Cart> {
    const cart = await this.cartRepository.getCartByUserIdWithRelations(userId);

    if (!cart) {
      throw new CartNotFoundException('userId', userId);
    }

    return cart;
  }
}
