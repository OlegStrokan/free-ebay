import { Payment, PaymentData } from 'src/checkout/core/entity/payment/payment';
import { PaymentDb } from '../../entity/payment.entity';
import { IMoneyMapper } from 'src/product/infrastructure/mappers/money/money.mapper.interface';
import { IPaymentMapper } from './payment.mapper.inteface';
import { Money } from 'src/shared/types/money';
import { OrderDb } from '../../entity/order.entity';
import { Injectable } from '@nestjs/common';

@Injectable()
export class PaymentMapper implements IPaymentMapper {
  constructor(private readonly moneyMapper: IMoneyMapper) {}

  toDomain(paymentDb: PaymentDb): Payment {
    if (!paymentDb.order?.id) {
      throw new Error(
        `Payment with id ${paymentDb.id} is missing its associated order. Ensure the order relation is loaded.`,
      );
    }

    const paymentData: PaymentData = {
      id: paymentDb.id,
      orderId: paymentDb.order.id,
      amount:
        this.moneyMapper.toDomain(paymentDb.amount) ?? Money.getDefaultMoney(),
      paymentMethod: paymentDb.paymentMethod,
      status: paymentDb.paymentStatus,
      createdAt: paymentDb.createdAt,
      updatedAt: paymentDb.updatedAt,
    };

    return new Payment(paymentData);
  }

  toDb(payment: PaymentData): PaymentDb {
    const paymentDb = new PaymentDb();
    paymentDb.id = payment.id;
    paymentDb.amount = this.moneyMapper.toDb(payment.amount);
    paymentDb.paymentMethod = payment.paymentMethod;
    paymentDb.paymentStatus = payment.status;
    paymentDb.createdAt = payment.createdAt;
    paymentDb.updatedAt = payment.updatedAt;
    paymentDb.order = { id: payment.orderId } as OrderDb;

    return paymentDb;
  }

  toClient(payment: Payment): PaymentData {
    return payment.data;
  }
}
