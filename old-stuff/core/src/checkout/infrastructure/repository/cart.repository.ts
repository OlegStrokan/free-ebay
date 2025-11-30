import { Injectable } from '@nestjs/common';
import { Repository } from 'typeorm';
import { InjectRepository } from '@nestjs/typeorm';
import { CartDb } from '../entity/cart.entity';
import { Cart } from 'src/checkout/core/entity/cart/cart';
import { ICartMapper } from '../mappers/cart/cart.mapper.interface';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import { IClearableRepository } from 'src/shared/types/clearable';
import { CartItemDb } from '../entity/cart-item.entity';

@Injectable()
export class CartRepository implements ICartRepository, IClearableRepository {
  constructor(
    @InjectRepository(CartDb)
    private readonly cartRepository: Repository<CartDb>,
    @InjectRepository(CartItemDb)
    private readonly cartItemRepository: Repository<CartItemDb>,
    private readonly mapper: ICartMapper,
  ) {}

  async saveCart(cart: Cart): Promise<Cart> {
    const dbCart = this.mapper.toDb(cart);
    const savedDbCart = await this.cartRepository.save(dbCart);
    return this.mapper.toDomain(savedDbCart);
  }
  async updateCart(cart: Cart): Promise<Cart> {
    const dbCart = this.mapper.toDb(cart);

    if (dbCart.items.length === 0) {
      await this.cartItemRepository.delete({ cart: dbCart });
    }

    const savedCart = await this.cartRepository.save(dbCart);
    return this.mapper.toDomain(savedCart);
  }
  async getCartByUserId(userId: string): Promise<Cart | null> {
    const cart = await this.cartRepository.findOneBy({ userId });

    return cart ? this.mapper.toDomain(cart) : null;
  }

  async getCartByUserIdWithRelations(userId: string): Promise<Cart | null> {
    const cart = await this.cartRepository.findOne({
      where: { userId },
      relations: ['items'],
    });
    return cart ? this.mapper.toDomain(cart) : null;
  }

  async getOneByIdIdWithRelations(id: string): Promise<Cart | null> {
    const cart = await this.cartRepository.findOne({
      where: { id },
      relations: ['items'],
    });
    return cart ? this.mapper.toDomain(cart) : null;
  }

  async getCartById(id: string): Promise<Cart | null> {
    const cart = await this.cartRepository.findOneBy({ id });
    return cart ? this.mapper.toDomain(cart) : null;
  }

  async clear(): Promise<void> {
    await this.cartRepository.query(`DELETE FROM "carts"`);
  }
}
