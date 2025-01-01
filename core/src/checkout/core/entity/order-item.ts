import { Clonable } from 'src/shared/types/clonable';
import { Money } from 'src/shared/types/money';

export interface CartItemData {
  productId: string;
  quantity: number;
  price: Money;
}

export class CartItem implements Clonable<CartItem> {
  constructor(public item: CartItemData) {}

  get productId(): string {
    return this.item.productId;
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
