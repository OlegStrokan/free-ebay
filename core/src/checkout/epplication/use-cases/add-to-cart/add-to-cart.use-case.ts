import { Injectable } from '@nestjs/common';
import { IAddToCartUseCase } from './add-to-cart.interface';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import { AddToCartDto } from 'src/checkout/interface/dtos/add-to-cart.dto';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';
import { CartItem } from 'src/checkout/core/entity/cart-item/cart-item';
import { Cart } from 'src/checkout/core/entity/cart/cart';

@Injectable()
export class AddToCartUseCase implements IAddToCartUseCase {
  constructor(
    private readonly cartRepository: ICartRepository,
    private readonly productRepository: IProductRepository,
  ) {}

  async execute(dto: AddToCartDto): Promise<Cart> {
    const cart = await this.cartRepository.getOneByIdIdWithRelations(
      dto.cartId,
    );
    if (!cart) {
      throw new CartNotFoundException('id', dto.cartId);
    }

    const product = await this.productRepository.findById(dto.productId);
    if (!product) {
      throw new ProductNotFoundException('id', dto.productId);
    }

    const cartItem = CartItem.create({ ...dto, price: product.price });

    const newCart = cart.addItem(cartItem.data);
    return await this.cartRepository.updateCart(newCart);
  }
}
