import { Money } from 'src/shared/types/money';
import { CartDb } from '../../entity/cart.entity';
import { IMoneyMapper } from 'src/product/infrastructure/mappers/money/money.mapper.interface';
import { CartItemDb } from '../../entity/cart-item.entity';
import { ICartMapper } from './cart.mapper.interface';
import { Cart } from 'src/checkout/core/entity/cart/cart';
import { CartData } from 'src/checkout/core/entity/cart/cart';
import { CartDto } from 'src/checkout/interface/dtos/cart.dto';
import { MoneyDto } from 'src/shared/types/money.dto';
import { Injectable } from '@nestjs/common';

@Injectable()
export class CartMapper implements ICartMapper {
  constructor(private readonly moneyMapper: IMoneyMapper) {}

  toDomain(cartDb: CartDb): Cart {
    const cartData: CartData = {
      id: cartDb.id,
      userId: cartDb.userId,
      items: cartDb.items?.map((cartItem) => ({
        id: cartItem.id,
        productId: cartItem.productId,
        cartId: cartItem.cartId,
        quantity: cartItem.quantity,
        createdAt: cartItem.createdAt,
        updatedAt: cartItem.updatedAt,
        price:
          this.moneyMapper.toDomain(cartItem.price) ?? Money.getDefaultMoney(),
      })),
      totalPrice:
        this.moneyMapper.toDomain(cartDb.totalPrice) ?? Money.getDefaultMoney(),
      createdAt: cartDb.createdAt,
      updatedAt: cartDb.updatedAt,
    };

    return new Cart(cartData);
  }

  toDb(cart: Cart): CartDb {
    const cartDb = new CartDb();
    cartDb.id = cart.id;
    cartDb.userId = cart.userId;
    cartDb.items = cart.items.map((item) => {
      const cartItem = new CartItemDb();
      cartItem.id = item.id;
      cartItem.quantity = item.quantity;
      cartItem.cartId = item.cartId;
      cartItem.createdAt = item.createdAt;
      cartItem.productId = item.productId;
      cartItem.updatedAt = item.updatedAt;
      cartItem.price = this.moneyMapper.toDb(item.price) ?? '';
      return cartItem;
    });
    cartDb.totalPrice = JSON.stringify(cart.totalPrice);
    cartDb.createdAt = cart.cart.createdAt;
    cartDb.updatedAt = cart.cart.updatedAt;

    return cartDb;
  }

  toClient(cart: Cart): CartDto {
    return {
      id: cart.id,
      userId: cart.userId,
      items: cart.items.map((item) => ({
        id: item.id,
        productId: item.productId,
        cartId: item.cartId,
        quantity: item.quantity,
        price: this.moneyMapper.toClient(item.price) as MoneyDto,
        createdAt: item.createdAt,
        updatedAt: item.updatedAt,
      })),
      totalPrice: this.moneyMapper.toClient(cart.totalPrice) as MoneyDto,
      createdAt: cart.cart.createdAt,
      updatedAt: cart.cart.updatedAt,
    };
  }
}
