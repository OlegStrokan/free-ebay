import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { IOrderMockService } from 'src/checkout/core/entity/order/mocks/order-mock.interface';
import { ICancelOrderUseCase } from './cancel-order.interface';
import { OrderNotFoundException } from 'src/checkout/core/exceptions/order/order-not-found.exception';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { OrderStatus } from 'src/checkout/core/entity/order/order';
import { IOrderItemMockService } from 'src/checkout/core/entity/order-item/mocks/order-item-mock.interface';

describe('CancelOrderUseCase', () => {
  let cancelOrderUseCase: ICancelOrderUseCase;
  let orderMockService: IOrderMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    cancelOrderUseCase = module.get(ICancelOrderUseCase);
    orderMockService = module.get(IOrderItemMockService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully cancel an order', async () => {
    const userId = generateUlid();
    const orderId = generateUlid();
    const order = await orderMockService.createOneWithDependencies(
      {
        id: orderId,
        status: OrderStatus.Shipped,
        userId,
      },
      { id: userId },
      [{ orderId }],
      1,
    );

    const canceledOrder = await cancelOrderUseCase.execute(order.id);

    expect(canceledOrder.status).toBe(OrderStatus.Cancelled);
  });

  it('should throw OrderNotFoundException if order does not exist', async () => {
    const nonExistentOrderId = generateUlid();

    await expect(
      cancelOrderUseCase.execute(nonExistentOrderId),
    ).rejects.toThrow(OrderNotFoundException);
  });
});
