import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';

export interface CartItemData {
  id: string;
  productId: string;
  cartId: string;
  quantity: number;
  price: Money;
  createdAt: Date;
  updatedAt: Date;
}

export class CartItem implements Clonable<CartItem> {
  constructor(public item: CartItemData) {}

  static create(
    itemData: Omit<CartItemData, 'id' | 'createdAt' | 'updatedAt'>,
  ) {
    return new CartItem({
      ...itemData,
      id: generateUlid(),
      createdAt: new Date(),
      updatedAt: new Date(),
    });
  }

  get id(): string {
    return this.item.id;
  }

  get data(): CartItemData {
    return this.item;
  }

  get productId(): string {
    return this.item.productId;
  }

  get cartId(): string {
    return this.item.cartId;
  }

  get quantity(): number {
    return this.item.quantity;
  }

  get price(): Money {
    return this.item.price;
  }

  updateQuantity = (newQuantity: number) => {
    const clone = this.clone();
    clone.item.quantity = newQuantity;
    return clone;
  };

  updatePrice = (newPrice: Money) => {
    const clone = this.clone();
    clone.item.price = newPrice;
    return clone;
  };

  clone = (): CartItem => new CartItem({ ...this.item });
}
