// core/src/checkout/core/entity/cart-item/cart-item.spec.ts
import { CartItem, CartItemData } from './cart-item';
import { Money } from 'src/shared/types/money';
import { generateUlid } from 'src/shared/types/generate-ulid';

describe('CartItem', () => {
  let cartItemData: CartItemData;
  let cartItem: CartItem;

  beforeEach(() => {
    cartItemData = {
      id: generateUlid(),
      productId: 'product1',
      cartId: 'cart1',
      quantity: 2,
      price: new Money(100, 'USD', 2),
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    cartItem = new CartItem(cartItemData);
  });

  test('should create a cart item successfully', () => {
    const newItem = CartItem.create({
      productId: 'product2',
      cartId: 'cart2',
      quantity: 1,
      price: new Money(50, 'USD', 2),
    });
    expect(newItem).toBeInstanceOf(CartItem);
    expect(newItem.productId).toBe('product2');
    expect(newItem.cartId).toBe('cart2');
    expect(newItem.quantity).toBe(1);
    expect(newItem.price.getAmount()).toBe(50);
  });

  test('should update quantity successfully', () => {
    const updatedItem = cartItem.updateQuantity(3);
    expect(updatedItem.quantity).toBe(3);
  });

  test('should update price successfully', () => {
    const newPrice = new Money(120, 'USD', 2);
    const updatedItem = cartItem.updatePrice(newPrice);
    expect(updatedItem.price.getAmount()).toBe(120);
  });

  test('should retain original item data after update', () => {
    const updatedItem = cartItem.updateQuantity(3);
    expect(cartItem.quantity).toBe(2);
    expect(updatedItem.quantity).toBe(3);
  });

  test('should retain original item data after price update', () => {
    const newPrice = new Money(120, 'USD', 2);
    const updatedItem = cartItem.updatePrice(newPrice);
    expect(cartItem.price.getAmount()).toBe(100);
    expect(updatedItem.price.getAmount()).toBe(120);
  });
});
