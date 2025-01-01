import { Payment } from '../entity/payment';

export interface IPaymentRepository {
  createPayment(paymentData: Partial<Payment>): Promise<Payment>;
  findById(paymentId: string): Promise<Payment>;
  updatePaymentStatus(paymentId: string, status: string): Promise<Payment>;
  findPaymentsByOrderId(orderId: string): Promise<Payment[]>;
}

export interface IPaymentRepository {
  proceedPayment(dto: any): Promise<any>;
  checkPaymentStatus(id: string): Promise<any>;
}
