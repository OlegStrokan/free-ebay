import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';
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
    return items.reduce((total, item) => total.add(item.price), new Money(0));
  }

  clone = (): Cart => new Cart({ ...this.cart });
}
