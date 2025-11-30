import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { ICheckPaymentStatusUseCase } from './check-payment-status.interface';
import { IPaymentMockService } from 'src/checkout/core/entity/payment/mocks/payment-mock.inteface';
import { PaymentStatus } from 'src/checkout/core/entity/payment/payment';
import { PaymentNotFoundException } from 'src/checkout/core/exceptions/payment/payment-not-found.exception';
import { IOrderMockService } from 'src/checkout/core/entity/order/mocks/order-mock.interface';
import { OrderStatus } from 'src/checkout/core/entity/order/order';

describe('CheckPaymentStatusUseCaseTest', () => {
  let checkPaymentStatusUseCase: ICheckPaymentStatusUseCase;
  let paymentMockService: IPaymentMockService;
  let orderMockService: IOrderMockService;

  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    checkPaymentStatusUseCase = module.get(ICheckPaymentStatusUseCase);
    paymentMockService = module.get(IPaymentMockService);
    orderMockService = module.get(IOrderMockService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully retrieve payment status', async () => {
    const userId = generateUlid();
    const orderId = generateUlid();
    await orderMockService.createOneWithDependencies(
      {
        id: orderId,
        status: OrderStatus.Shipped,
        userId,
      },
      { id: userId },
      [{ orderId }],
      1,
    );

    const paymentId = generateUlid();
    const payment = await paymentMockService.createOne({
      id: paymentId,
      status: PaymentStatus.Pending,
      orderId: orderId,
    });

    const paymentStatus = await checkPaymentStatusUseCase.execute(payment.id);

    expect(paymentStatus).toBe(PaymentStatus.Pending);
  });

  it("should throw exception if payment doesn't exist", async () => {
    const nonExistedPaymentId = generateUlid();

    await expect(
      checkPaymentStatusUseCase.execute(nonExistedPaymentId),
    ).rejects.toThrow(PaymentNotFoundException);
  });
});
