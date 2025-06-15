import { Injectable } from '@nestjs/common';
import { IRetrieveCartUseCase } from './retrieve-cart.interface';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import { Cart } from 'src/checkout/core/entity/cart/cart';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';

@Injectable()
export class RetrieveCartUseCase implements IRetrieveCartUseCase {
  constructor(private readonly cartRepository: ICartRepository) {}

  async execute(userId: string): Promise<Cart> {
    const cart = await this.cartRepository.getCartByUserIdWithRelations(userId);

    if (!cart) {
      throw new CartNotFoundException('userId', userId);
    }

    return cart;
  }
}
