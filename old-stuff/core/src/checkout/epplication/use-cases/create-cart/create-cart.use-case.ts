import { Injectable } from '@nestjs/common';
import { ICreateCartUseCase } from './create-cart.interface';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import { Cart } from 'src/checkout/core/entity/cart/cart';
import { CreateCartDto } from 'src/checkout/interface/dtos/create-cart.dto';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';
import { CartAlreadyExists } from 'src/checkout/core/exceptions/cart/cart-already-exist.exception';

@Injectable()
export class CreateCartUseCase implements ICreateCartUseCase {
  constructor(
    private readonly cartRepository: ICartRepository,
    private readonly userRepository: IUserRepository,
  ) {}
  async execute(dto: CreateCartDto): Promise<Cart> {
    const user = await this.userRepository.findById(dto.userId);
    if (!user) {
      throw new UserNotFoundException('id', dto.userId);
    }
    const existingCart = await this.cartRepository.getCartByUserId(dto.userId);
    if (existingCart) {
      throw new CartAlreadyExists(dto.userId);
    }
    const cart = Cart.create(dto.userId);
    return await this.cartRepository.saveCart(cart);
  }
}
