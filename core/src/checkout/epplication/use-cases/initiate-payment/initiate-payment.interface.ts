import { Payment } from 'src/checkout/core/entity/payment/payment';
import { Shipment } from 'src/checkout/core/entity/shipment/shipment';
import { ProceedPaymentDto } from 'src/checkout/interface/dtos/proceed-payment.dto';

export interface PaymentResult {
  shipment: Shipment;
  payment: Payment;
}

export abstract class IInitiatePaymentUseCase {
  abstract execute(payment: ProceedPaymentDto): Promise<PaymentResult>;
}
