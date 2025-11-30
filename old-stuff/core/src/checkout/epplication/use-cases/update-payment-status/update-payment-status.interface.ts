import { PaymentStatus } from 'src/checkout/core/entity/payment/payment';

export abstract class IUpdatePaymentStatusUseCase {
  abstract execute(
    paymentIntentId: string,
    newStatus: PaymentStatus,
  ): Promise<void>;
}
