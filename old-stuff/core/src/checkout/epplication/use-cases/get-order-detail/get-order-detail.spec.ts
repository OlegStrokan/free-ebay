import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { IOrderMockService } from 'src/checkout/core/entity/order/mocks/order-mock.interface';
import { OrderNotFoundException } from 'src/checkout/core/exceptions/order/order-not-found.exception';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { OrderStatus } from 'src/checkout/core/entity/order/order';
import { IGetOrderDetailsUseCase } from './get-order-detail.interface';
import { validateOrderDataStructure } from 'src/checkout/infrastructure/mappers/order/order.mapper.spec';

describe('GetOrderDetailUseCase', () => {
  let getOrderDetailUseCase: IGetOrderDetailsUseCase;
  let orderMockService: IOrderMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getOrderDetailUseCase = module.get(IGetOrderDetailsUseCase);
    orderMockService = module.get(IOrderMockService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully retrieve order detail', async () => {
    const userId = generateUlid();
    const order = await orderMockService.createOneWithDependencies(
      {
        status: OrderStatus.Pending,
        userId,
      },
      { id: userId },
    );

    const retrievedOrder = await getOrderDetailUseCase.execute(order.id);

    expect(retrievedOrder).toBeDefined();
    validateOrderDataStructure(retrievedOrder.data);
  });

  it('should throw OrderNotFoundException if order does not exist', async () => {
    const nonExistentOrderId = generateUlid();

    await expect(
      getOrderDetailUseCase.execute(nonExistentOrderId),
    ).rejects.toThrow(OrderNotFoundException);
  });
});
