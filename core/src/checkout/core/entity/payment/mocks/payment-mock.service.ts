import { Inject, Injectable } from '@nestjs/common';

import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';
import { CreatePaymentDto } from 'src/checkout/interface/dtos/create-payment.dto';
import { faker } from '@faker-js/faker';
import { PAYMENT_REPOSITORY } from 'src/checkout/epplication/injection-tokens/repository.token';
import { PaymentMethod, PaymentData, Payment, PaymentStatus } from '../payment';
import { IPaymentMockService } from './payment-mock.inteface';

@Injectable()
export class PaymentMockService implements IPaymentMockService {
  constructor(
    @Inject(PAYMENT_REPOSITORY)
    private readonly paymentRepository: IPaymentRepository,
  ) {}

  getOneToCreate(overrides: Partial<CreatePaymentDto> = {}): CreatePaymentDto {
    return {
      orderId: overrides?.orderId ?? generateUlid().toString(),
      amount:
        overrides?.amount ?? new Money(faker.number.int(1000), 'USD', 100),
      paymentMethod:
        overrides?.paymentMethod ?? faker.helpers.enumValue(PaymentMethod),
    };
  }

  getOne(overrides: Partial<PaymentData> = {}): Payment {
    const paymentData: PaymentData = {
      id: overrides.id ?? generateUlid(),
      orderId: overrides.orderId ?? generateUlid(),
      amount: overrides.amount ?? new Money(faker.number.int(1000), 'USD', 100),
      paymentMethod:
        overrides.paymentMethod ?? faker.helpers.enumValue(PaymentMethod),
      status: overrides.status ?? faker.helpers.enumValue(PaymentStatus),
      createdAt: overrides.createdAt ?? new Date(),
      updatedAt: overrides.updatedAt ?? new Date(),
    };

    return new Payment(paymentData);
  }

  async createOne(overrides: Partial<PaymentData> = {}): Promise<Payment> {
    const payment = this.getOne(overrides);
    return await this.paymentRepository.save(payment);
  }
}
