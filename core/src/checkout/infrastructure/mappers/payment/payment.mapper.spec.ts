import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import {
  Payment,
  PaymentData,
  PaymentStatus,
  PaymentMethod,
} from 'src/checkout/core/entity/payment/payment';
import { PaymentDb } from '../../entity/payment.entity';
import { Money } from 'src/shared/types/money';
import { PAYMENT_MAPPER } from 'src/checkout/epplication/injection-tokens/mapper.token';
import { IPaymentMapper } from './payment.mapper.inteface';

export const validatePaymentDataStructure = (
  paymentData: PaymentData | undefined,
) => {
  if (!paymentData) throw new Error('Payment not found test error');

  expect(paymentData).toEqual({
    id: expect.any(String),
    orderId: expect.any(String),
    amount: expect.objectContaining({
      amount: expect.any(Number),
      currency: expect.any(String),
      fraction: expect.any(Number),
    }),
    paymentMethod: expect.any(String),
    status: expect.any(String),
    createdAt: expect.any(Date),
    updatedAt: expect.any(Date),
  });
};

describe('PaymentMapperTest', () => {
  let module: TestingModule;
  let paymentMapper: IPaymentMapper<PaymentData, Payment, PaymentDb>;

  beforeAll(async () => {
    module = await createTestingModule();

    paymentMapper =
      module.get<IPaymentMapper<PaymentData, Payment, PaymentDb>>(
        PAYMENT_MAPPER,
      );
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully transform domain payment to client (dto) payment', async () => {
    const domainPayment = new Payment({
      id: 'payment123',
      orderId: 'order123',
      amount: new Money(100, 'USD', 100),
      paymentMethod: PaymentMethod.CreditCard,
      status: PaymentStatus.Pending,
      createdAt: new Date(),
      updatedAt: new Date(),
    });

    const dtoPayment = paymentMapper.toClient(domainPayment);
    validatePaymentDataStructure(dtoPayment);
  });

  it('should successfully map database payment to domain payment', async () => {
    const paymentDb = new PaymentDb();
    paymentDb.id = 'payment123';
    paymentDb.order = { id: 'order123' } as any; // Assuming order is loaded
    paymentDb.amount = JSON.stringify({
      amount: 100,
      currency: 'USD',
      fraction: 100,
    });
    paymentDb.paymentMethod = PaymentMethod.CreditCard;
    paymentDb.paymentStatus = PaymentStatus.Pending;
    paymentDb.createdAt = new Date();
    paymentDb.updatedAt = new Date();

    const domainPayment = paymentMapper.toDomain(paymentDb);
    validatePaymentDataStructure(domainPayment.data);
  });
});
