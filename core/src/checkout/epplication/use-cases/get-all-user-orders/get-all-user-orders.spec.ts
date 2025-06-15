import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { IOrderMockService } from 'src/checkout/core/entity/order/mocks/order-mock.interface';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { OrderStatus } from 'src/checkout/core/entity/order/order';
import { IGetAllUserOrdersUseCase } from './get-all-user-orders.interface';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';

describe('GetAllUserOrdersUseCase', () => {
  let getAllUserOrdersUseCase: IGetAllUserOrdersUseCase;
  let orderMockService: IOrderMockService;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getAllUserOrdersUseCase = module.get(IGetAllUserOrdersUseCase);
    orderMockService = module.get(IOrderMockService);
    userMockService = module.get(IUserMockService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully retrieve all user orders', async () => {
    const userId = generateUlid();
    await userMockService.createOne({ id: userId });
    await Promise.all([
      (orderMockService.createOne({
        status: OrderStatus.Pending,
        userId,
      }),
      orderMockService.createOneWithDependencies(
        {
          status: OrderStatus.Pending,
          userId,
        },
        { id: userId },
      )),
    ]);

    const userOrders = await getAllUserOrdersUseCase.execute(userId);

    expect(userOrders.length).toBe(2);
    userOrders.map((order) => expect(order.status).toBe(OrderStatus.Pending));
    userOrders.map((order) => expect(order.userId).toBe(userId));
  });
});
