import { Payment } from '../entity/payment/payment';

export abstract class IPaymentRepository {
  abstract save(payment: Payment): Promise<Payment>;
  abstract findById(paymentId: string): Promise<Payment | null>;
  abstract update(payment: Payment): Promise<Payment>;
  abstract findPaymentsByOrderId(orderId: string): Promise<Payment[]>;
  abstract findByPaymentIntentId(
    paymentIntentId: string,
  ): Promise<Payment | null>;
}
