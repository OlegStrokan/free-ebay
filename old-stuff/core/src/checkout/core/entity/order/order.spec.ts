import { Order, OrderData, OrderStatus } from './order';
import { Money } from 'src/shared/types/money';
import { OrderItem } from '../order-item/order-item';
import { IOrderItemMockService } from '../order-item/mocks/order-item-mock.interface';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { TestingModule } from '@nestjs/testing';
import { InvalidOrderItemsException } from '../../exceptions/order/invalid-order-items.exception';

describe('Order', () => {
  let orderData: OrderData;
  let order: Order;
  let orderItemMockService: IOrderItemMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    orderItemMockService = module.get(IOrderItemMockService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(() => {
    orderData = {
      id: '1',
      userId: 'user1',
      status: OrderStatus.Pending,
      items: [],
      totalPrice: new Money(0, 'USD', 100),
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    order = new Order(orderData);
  });

  test('should create an order successfully', () => {
    const newOrder = Order.create({
      userId: 'user2',
      totalPrice: new Money(200, 'USD', 100),
    });
    expect(newOrder).toBeInstanceOf(Order);
    expect(newOrder.data.userId).toBe('user2');
    expect(newOrder.data.status).toBe(OrderStatus.Pending);
    expect(newOrder.data.items).toHaveLength(0);
  });

  test('should add an item successfully', () => {
    const item: OrderItem = orderItemMockService.getOne({
      id: 'item1',
      priceAtPurchase: new Money(50, 'USD', 100),
    });
    const updatedOrder = order.addItem(item.data);
    expect(updatedOrder.items).toHaveLength(1);
    expect(updatedOrder.totalPrice.getAmount()).toBe(50);
  });

  test('should throw InvalidOrderItems exception in addItem', () => {
    const item: OrderItem = orderItemMockService.getOne({
      id: 'item1',
      quantity: 0,
      priceAtPurchase: new Money(50, 'USD', 100),
    });
    expect(() => order.addItem(item.data)).toThrow(InvalidOrderItemsException);
  });

  test('should remove an item successfully', () => {
    const item: OrderItem = orderItemMockService.getOne({
      id: 'item1',
      priceAtPurchase: new Money(50, 'USD', 100),
    });
    order = order.addItem(item.data);
    const updatedOrder = order.removeItem('item1');
    expect(updatedOrder.items).toHaveLength(0);
    expect(updatedOrder.totalPrice.getAmount()).toBe(0);
  });

  test('should apply discount successfully', () => {
    const item: OrderItem = orderItemMockService.getOne({
      id: 'item1',
      priceAtPurchase: new Money(50, 'USD', 100),
    });
    order = order.addItem(item.data);
    const updatedOrder = order.applyDiscount(10);
    expect(updatedOrder.totalPrice.getAmount()).toBe(45);
  });

  test('should calculate taxes successfully', () => {
    const item: OrderItem = orderItemMockService.getOne({
      id: 'item1',
      priceAtPurchase: new Money(50, 'USD', 100),
    });
    order = order.addItem(item.data);
    const updatedOrder = order.calculateTaxes(10);
    expect(updatedOrder.totalPrice.getAmount()).toBe(55);
  });

  test('should update status successfully', () => {
    const updatedOrder = order.updateStatus(OrderStatus.Shipped);
    expect(updatedOrder.data.status).toBe(OrderStatus.Shipped);
  });

  test('should throw InvalidOrderItems exception in addItems', () => {
    const [item1, item2]: OrderItem[] = orderItemMockService.getMany(2, [
      {
        id: 'item2',
        priceAtPurchase: new Money(30, 'USD', 100),
      },
      { id: 'item1', priceAtPurchase: new Money(50, 'USD', 100) },
    ]);

    const updatedOrder = order.addItems([item1.data, item2.data]);
    expect(updatedOrder.items).toHaveLength(2);
    expect(updatedOrder.totalPrice.getAmount()).toBe(80);
  });
  test('should handle adding multiple items', () => {
    const [item1, item2]: OrderItem[] = orderItemMockService.getMany(2, [
      {
        id: 'item2',
        priceAtPurchase: new Money(30, 'USD', 100),
        quantity: 0,
      },
      { id: 'item1', priceAtPurchase: new Money(50, 'USD', 100) },
    ]);

    expect(() => order.addItems([item1.data, item2.data])).toThrow(
      InvalidOrderItemsException,
    );
  });

  test('should handle removing multiple items', () => {
    const [item1, item2]: OrderItem[] = orderItemMockService.getMany(2, [
      {
        id: 'item2',
        priceAtPurchase: new Money(30, 'USD', 100),
      },
      { id: 'item1', priceAtPurchase: new Money(50, 'USD', 100) },
    ]);
    order = order.addItems([item1.data, item2.data]);
    const updatedOrder = order.removeItems(['item1', 'item2']);
    expect(updatedOrder.items).toHaveLength(0);
    expect(updatedOrder.totalPrice.getAmount()).toBe(0);
  });
});
