import { PaymentStatus } from '../entity/payment-status';

export interface IPaymentStatusRepository {
  createStatus(statusData: Partial<PaymentStatus>): Promise<PaymentStatus>;
  findById(statusId: string): Promise<PaymentStatus>;
  findByPaymentId(paymentId: string): Promise<PaymentStatus[]>;
}
