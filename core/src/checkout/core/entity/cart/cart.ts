/* eslint-disable @typescript-eslint/ban-ts-comment */
import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { CartItemData } from '../cart-item/cart-item';
import { Money } from 'src/shared/types/money';

export interface CartData {
  id: string;
  userId: string;
  items: CartItemData[];
  totalPrice: Money;
  createdAt: Date;
  updatedAt: Date;
}

export class Cart implements Clonable<Cart> {
  constructor(public cart: CartData) {}

  /**
   * Creation of cart require only userId, because we create cart when user open application and didn't add it to cart
   */

  static create = (userId: string): Cart => {
    return new Cart({
      userId,
      id: generateUlid(),
      createdAt: new Date(),
      updatedAt: new Date(),
      totalPrice: Money.getDefaultMoney(),
      items: [],
    });
  };

  get id(): string {
    return this.cart.id;
  }

  get data(): CartData {
    return this.cart;
  }

  get userId(): string {
    return this.cart.userId;
  }

  get items(): CartItemData[] {
    return this.cart.items;
  }

  get totalPrice(): Money {
    return this.cart.totalPrice;
  }

  addItem = (item: CartItemData) => {
    const clone = this.clone();
    clone.cart.items.push(item);
    clone.cart.totalPrice = this.calculateTotalPrice(clone.cart.items);
    return clone;
  };

  removeItem = (productId: string) => {
    const clone = this.clone();
    clone.cart.items = clone.cart.items.filter(
      (item) => item.productId !== productId,
    );
    clone.cart.totalPrice = this.calculateTotalPrice(clone.cart.items);
    return clone;
  };

  private calculateTotalPrice(items: CartItemData[]): Money {
    if (items.length === 0) {
      return Money.getDefaultMoney();
    }

    const initialMoney = Money.getDefaultMoney();

    return items.reduce((total, item) => total.add(item.price), initialMoney);
  }

  clone = (): Cart => new Cart({ ...this.cart });
}
