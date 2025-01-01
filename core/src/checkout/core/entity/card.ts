import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { addMoney, createMoney, Money } from 'src/shared/types/money';
import { CartItemData } from './order-item';

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

  static create = (
    cartData: Omit<CartData, 'id' | 'createdAt' | 'updatedAt'>,
  ) =>
    new Cart({
      ...cartData,
      id: generateUlid(),
      createdAt: new Date(),
      updatedAt: new Date(),
    });

  get id(): string {
    return this.cart.id;
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

  addItem = (productId: string, quantity: number, price: Money) => {
    const clone = this.clone();
    const newItem = { productId, quantity, price };
    clone.cart.items.push(newItem);
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
      throw new Error(
        'Cannot calculate total price for an empty list of items',
      );
    }

    const initialMoney = createMoney(
      0,
      items[0].price.currency,
      items[0].price.fraction,
    );

    return items.reduce(
      (total, item) => addMoney(total, item.price),
      initialMoney,
    );
  }

  clone = (): Cart => new Cart({ ...this.cart });
}
