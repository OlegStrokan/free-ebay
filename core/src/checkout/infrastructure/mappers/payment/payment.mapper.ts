import { Inject } from '@nestjs/common';
import { Payment, PaymentData } from 'src/checkout/core/entity/payment/payment';
import { PaymentDb } from '../../entity/payment.entity';
import { MoneyMapper } from 'src/product/infrastructure/mappers/money/money.mapper';
import { IMoneyMapper } from 'src/product/infrastructure/mappers/money/money.mapper.interface';
import { IPaymentMapper } from './payment.mapper.inteface';
import { Money } from 'src/shared/types/money';
import { generateUlid } from 'src/shared/types/generate-ulid';

export class PaymentMapper
  implements IPaymentMapper<PaymentData, Payment, PaymentDb>
{
  constructor(
    @Inject(MoneyMapper)
    private readonly moneyMapper: IMoneyMapper,
  ) {}

  toDomain(paymentDb: PaymentDb): Payment {
    const paymentData: PaymentData = {
      id: paymentDb.id,
      // TODO fix it
      orderId: paymentDb?.order?.id ?? generateUlid(),
      amount:
        this.moneyMapper.toDomain(paymentDb.amount) ?? Money.getDefaultMoney(),
      paymentMethod: paymentDb.paymentMethod,
      status: paymentDb.paymentStatus,
      createdAt: paymentDb.createdAt,
      updatedAt: paymentDb.updatedAt,
    };

    return new Payment(paymentData);
  }

  toDb(payment: Payment): PaymentDb {
    const paymentDb = new PaymentDb();
    paymentDb.id = payment.id;
    // Assuming order is set elsewhere
    paymentDb.amount = this.moneyMapper.toDb(payment.amount);
    paymentDb.paymentMethod = payment.paymentMethod;
    paymentDb.paymentStatus = payment.payment.status;
    paymentDb.createdAt = payment.payment.createdAt;
    paymentDb.updatedAt = payment.payment.updatedAt;

    return paymentDb;
  }

  toClient(payment: Payment): PaymentData {
    return payment.data;
  }
}
