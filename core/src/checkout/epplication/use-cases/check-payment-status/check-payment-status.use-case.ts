import { Injectable } from '@nestjs/common';
import { ICheckPaymentStatusUseCase } from './check-payment-status.interface';
import { PaymentStatus } from 'src/checkout/core/entity/payment/payment';
import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { PaymentNotFoundException } from 'src/checkout/core/exceptions/payment/payment-not-found.exception';

@Injectable()
export class CheckPaymentStatusUseCase implements ICheckPaymentStatusUseCase {
  constructor(private readonly repository: IPaymentRepository) {}

  async execute(paymentId: string): Promise<PaymentStatus> {
    const payment = await this.repository.findById(paymentId);
    if (!payment) {
      throw new PaymentNotFoundException('id', paymentId);
    }
    return payment?.data.status;
  }
}
