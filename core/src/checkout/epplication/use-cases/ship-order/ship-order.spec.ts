import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { SHIP_ORDER_USE_CASE_TOKEN } from '../../injection-tokens/use-case.token';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { IOrderMockService } from 'src/checkout/core/entity/order/mocks/order-mock.interface';
import { OrderMockService } from 'src/checkout/core/entity/order/mocks/order-mock.service';
import { OrderNotFoundException } from 'src/checkout/core/exceptions/order/order-not-found.exception';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { OrderStatus } from 'src/checkout/core/entity/order/order';
import { IShipOrderUseCase } from './ship-order.interface';

describe('ShipOrderUseCaseTest', () => {
  let shipOrderUseCase: IShipOrderUseCase;
  let orderMockService: IOrderMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    shipOrderUseCase = module.get(SHIP_ORDER_USE_CASE_TOKEN);
    orderMockService = module.get(OrderMockService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully ship an order', async () => {
    const userId = generateUlid();
    const order = await orderMockService.createOneWithDependencies(
      {
        status: OrderStatus.Pending,
        userId,
      },
      { id: userId },
    );

    const canceledOrder = await shipOrderUseCase.execute(order.id);

    expect(canceledOrder.status).toBe(OrderStatus.Shipped);
  });

  it('should throw OrderNotFoundException if order does not exist', async () => {
    const nonExistentOrderId = generateUlid();

    await expect(shipOrderUseCase.execute(nonExistentOrderId)).rejects.toThrow(
      OrderNotFoundException,
    );
  });
});
