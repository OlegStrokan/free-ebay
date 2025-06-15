import { CreatePaymentDto } from 'src/checkout/interface/dtos/create-payment.dto';
import { Payment, PaymentData } from '../payment';

export abstract class IPaymentMockService {
  abstract getOne(overrides?: Partial<PaymentData>): Payment;
  abstract createOne(overrides?: Partial<PaymentData>): Promise<Payment>;
  abstract getOneToCreate(
    overrides?: Partial<CreatePaymentDto>,
  ): CreatePaymentDto;
}
