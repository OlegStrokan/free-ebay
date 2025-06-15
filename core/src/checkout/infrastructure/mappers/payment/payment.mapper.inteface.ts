import { Payment } from 'src/checkout/core/entity/payment/payment';
import { PaymentDb } from '../../entity/payment.entity';
import { PaymentData } from 'src/checkout/core/entity/payment/payment';

export abstract class IPaymentMapper {
  abstract toDb(domain: PaymentData): PaymentDb;
  abstract toDomain(db: PaymentDb): Payment;
  abstract toClient(domain: Payment): PaymentData;
}
