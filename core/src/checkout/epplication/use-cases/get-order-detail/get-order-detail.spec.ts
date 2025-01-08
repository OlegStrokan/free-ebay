import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { GET_ORDER_DETAIL_USE_CASE_TOKEN } from '../../injection-tokens/use-case.token';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { IOrderMockService } from 'src/checkout/core/entity/order/mocks/order-mock.interface';
import { OrderNotFoundException } from 'src/checkout/core/exceptions/order/order-not-found.exception';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { OrderStatus } from 'src/checkout/core/entity/order/order';
import { IGetOrderDetailsUseCase } from './get-order-detail.interface';
import { validateOrderDataStructure } from 'src/checkout/infrastructure/mappers/order/order.mapper.spec';
import { ORDER_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';

describe('GetOrderDetailUseCase', () => {
  let getOrderDetailUseCase: IGetOrderDetailsUseCase;
  let orderMockService: IOrderMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getOrderDetailUseCase = module.get(GET_ORDER_DETAIL_USE_CASE_TOKEN);
    orderMockService = module.get(ORDER_MOCK_SERVICE);

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
