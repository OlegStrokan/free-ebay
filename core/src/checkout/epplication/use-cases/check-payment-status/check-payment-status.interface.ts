import { PaymentStatus } from 'src/checkout/core/entity/payment/payment';

export abstract class ICheckPaymentStatusUseCase {
  abstract execute(paymentId: string): Promise<PaymentStatus>;
}
