import { Injectable } from '@nestjs/common';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { IRemoveFromCartUseCase } from './remove-from-cart.interface';
import { Cart } from 'src/checkout/core/entity/cart/cart';
import { RemoveFromCartDto } from 'src/checkout/interface/dtos/remove-from-cart.dto';
import { CartItemNotFoundException } from 'src/checkout/core/exceptions/cart/cart-item-not-found.exception';

@Injectable()
export class RemoveFromCartUseCase implements IRemoveFromCartUseCase {
  constructor(private readonly cartRepository: ICartRepository) {}

  async execute(dto: RemoveFromCartDto): Promise<Cart> {
    const cart = await this.cartRepository.getOneByIdIdWithRelations(
      dto.cartId,
    );
    if (!cart) {
      throw new CartNotFoundException('id', dto.cartId);
    }

    const cartItem = cart.items.find((item) => item.id === dto.cartItemId);
    if (!cartItem) {
      throw new CartItemNotFoundException('id', dto.cartItemId);
    }
    const newCart = cart.removeItem(cartItem.id);

    return await this.cartRepository.updateCart(newCart);
  }
}
