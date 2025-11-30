import { Injectable } from '@nestjs/common';
import { IClearCartUseCase } from './clear-cart.interface';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';

import { Cart } from 'src/checkout/core/entity/cart/cart';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';

@Injectable()
export class ClearCartUseCase implements IClearCartUseCase {
  constructor(private readonly cartRepository: ICartRepository) {}

  async execute(cartId: string): Promise<Cart> {
    const cart = await this.cartRepository.getOneByIdIdWithRelations(cartId);
    if (!cart) {
      throw new CartNotFoundException('userId', cartId);
    }
    const clearedCart = cart.clearCart();
    return await this.cartRepository.updateCart(clearedCart);
  }
}
