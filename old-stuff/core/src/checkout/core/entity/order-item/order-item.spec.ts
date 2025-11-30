import { OrderItem, OrderItemData } from './order-item';
import { Money } from 'src/shared/types/money';
import { generateUlid } from 'src/shared/types/generate-ulid';

describe('OrderItem', () => {
  let orderItemData: OrderItemData;
  let orderItem: OrderItem;

  beforeEach(() => {
    orderItemData = {
      id: generateUlid(),
      productId: 'product1',
      orderId: 'order1',
      quantity: 2,
      priceAtPurchase: new Money(100, 'USD', 2),
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    orderItem = new OrderItem(orderItemData);
  });

  test('should create an order item successfully', () => {
    const newItem = OrderItem.create({
      productId: 'product2',
      orderId: 'order2',
      quantity: 1,
      priceAtPurchase: new Money(50, 'USD', 2),
    });
    expect(newItem).toBeInstanceOf(OrderItem);
    expect(newItem.productId).toBe('product2');
    expect(newItem.orderId).toBe('order2');
    expect(newItem.quantity).toBe(1);
    expect(newItem.priceAtPurchase.getAmount()).toBe(50);
  });

  test('should update quantity successfully', () => {
    const updatedItem = orderItem.updateQuantity(3);
    expect(updatedItem.quantity).toBe(3);
  });

  test('should update price successfully', () => {
    const newPrice = new Money(120, 'USD', 2);
    const updatedItem = orderItem.updatePrice(newPrice);
    expect(updatedItem.priceAtPurchase.getAmount()).toBe(120);
  });

  test('should retain original item data after update', () => {
    const updatedItem = orderItem.updateQuantity(3);
    expect(orderItem.quantity).toBe(2);
    expect(updatedItem.quantity).toBe(3);
  });

  test('should retain original item data after price update', () => {
    const newPrice = new Money(120, 'USD', 2);
    const updatedItem = orderItem.updatePrice(newPrice);
    expect(orderItem.priceAtPurchase.getAmount()).toBe(100);
    expect(updatedItem.priceAtPurchase.getAmount()).toBe(120);
  });
});
