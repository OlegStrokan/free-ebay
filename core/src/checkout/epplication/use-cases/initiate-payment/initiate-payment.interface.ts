import {
  Payment,
  PaymentMethod,
} from 'src/checkout/core/entity/payment/payment';
import { Shipment } from 'src/checkout/core/entity/shipment/shipment';
import { Money } from 'src/shared/types/money';

export interface InitiatePaymentDto {
  orderId: string;
  paymentMethod: PaymentMethod;
  amount: Money;
  shippingAddress: string;
}

export interface PaymentResult {
  shipment: Shipment;
  payment: Payment;
}

export abstract class IInitiatePaymentUseCase {
  abstract execute(payment: InitiatePaymentDto): Promise<PaymentResult>;
}
