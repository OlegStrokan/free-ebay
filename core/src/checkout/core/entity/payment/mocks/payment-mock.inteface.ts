import { CreatePaymentDto } from 'src/checkout/interface/dtos/create-payment.dto';
import { Payment, PaymentData } from '../payment';

export interface IPaymentMockService {
  getOne(overrides?: Partial<PaymentData>): Payment;
  createOne(overrides?: Partial<PaymentData>): Promise<Payment>;
  getOneToCreate(overrides?: Partial<CreatePaymentDto>): CreatePaymentDto;
}
