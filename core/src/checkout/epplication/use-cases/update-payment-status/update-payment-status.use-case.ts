import { Injectable } from '@nestjs/common';
import { IUpdatePaymentStatusUseCase } from './update-payment-status.interface';
import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { PaymentStatus } from 'src/checkout/core/entity/payment/payment';
import { PaymentNotFoundException } from 'src/checkout/core/exceptions/payment/payment-not-found.exception';

@Injectable()
export class UpdatePaymentStatusUseCase implements IUpdatePaymentStatusUseCase {
  constructor(private readonly paymentRepository: IPaymentRepository) {}

  async execute(
    paymentIntentId: string,
    newStatus: PaymentStatus,
  ): Promise<void> {
    const payment = await this.paymentRepository.findByPaymentIntentId(
      paymentIntentId,
    );
    if (!payment) {
      throw new PaymentNotFoundException('paymentIntentId', paymentIntentId);
    }
    const updatedPayment = payment.updateStatus(newStatus);
    await this.paymentRepository.save(updatedPayment);
  }
}
