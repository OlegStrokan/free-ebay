import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IOrderMapper } from './order.mapper.interface';
import { OrderData, OrderStatus } from 'src/checkout/core/entity/order/order';
import { Order } from 'src/checkout/core/entity/order/order';
import { OrderDb } from '../../entity/order.entity';
import { Money } from 'src/shared/types/money';
import { OrderItemDb } from '../../entity/order-item.entity';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';

export const validateOrderDataStructure = (
  orderData: OrderData | undefined,
) => {
  if (!orderData) throw new Error('Order not found test error');
  expect(orderData).toEqual({
    id: expect.any(String),
    userId: expect.any(String),
    status: expect.any(String),
    items: expect.arrayContaining([
      expect.objectContaining({
        id: expect.any(String),
        productId: expect.any(String),
        orderId: expect.any(String),
        quantity: expect.any(Number),
        priceAtPurchase: expect.objectContaining({
          amount: expect.any(Number),
          currency: expect.any(String),
          fraction: expect.any(Number),
        }),
        createdAt: expect.any(Date),
        updatedAt: expect.any(Date),
      }),
    ]),
    totalPrice: expect.objectContaining({
      amount: expect.any(Number),
      currency: expect.any(String),
      fraction: expect.any(Number),
    }),
    createdAt: expect.any(Date),
    updatedAt: expect.any(Date),
  });
};

describe('OrderMapperTest', () => {
  let module: TestingModule;
  let orderMapper: IOrderMapper;

  beforeAll(async () => {
    module = await createTestingModule();

    orderMapper = module.get(IOrderMapper);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully transform domain order to client (dto) order', async () => {
    const domainOrder = new Order({
      id: generateUlid(),
      userId: generateUlid(),
      status: OrderStatus.Shipped,
      items: [
        {
          id: generateUlid(),
          productId: generateUlid(),
          orderId: generateUlid(),
          quantity: 2,
          priceAtPurchase: new Money(200, 'USD', 100),
          createdAt: new Date(),
          updatedAt: new Date(),
        },
      ],
      totalPrice: new Money(400, 'USD', 100),
      createdAt: new Date(),
      updatedAt: new Date(),
    });

    const dtoOrder = orderMapper.toClient(domainOrder);
    validateOrderDataStructure(dtoOrder);
  });

  it('should successfully map database order to domain order', async () => {
    const orderDb = new OrderDb();
    orderDb.id = generateUlid();
    orderDb.status = OrderStatus.Shipped;
    orderDb.user = { id: generateUlid() } as UserDb;
    orderDb.items = [
      {
        id: generateUlid(),
        productId: generateUlid(),
        orderId: generateUlid(),
        createdAt: new Date(),
        updatedAt: new Date(),
        quantity: 2,
        priceAtPurchase: JSON.stringify({
          amount: 200,
          currency: 'USD',
          fraction: 100,
        }),
      } as OrderItemDb,
    ];
    orderDb.totalPrice = JSON.stringify({
      amount: 400,
      currency: 'USD',
      fraction: 100,
    });
    orderDb.createdAt = new Date();
    orderDb.updatedAt = new Date();

    const domainOrder = orderMapper.toDomain(orderDb);
    validateOrderDataStructure(domainOrder.data);
  });
});
