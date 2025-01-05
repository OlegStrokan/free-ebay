import { Payment } from '../entity/payment/payment';

export interface IPaymentRepository {
  save(payment: Payment): Promise<Payment>;
  findById(paymentId: string): Promise<Payment | null>;
  update(payment: Payment): Promise<Payment>;
  findPaymentsByOrderId(orderId: string): Promise<Payment[]>;
}
