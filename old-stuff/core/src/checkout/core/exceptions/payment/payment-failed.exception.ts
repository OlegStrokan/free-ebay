import { HttpException, HttpStatus } from '@nestjs/common';

export class PaymentFailedException extends HttpException {
  constructor(orderId: string) {
    super(
      `Payment failed for order with id: ${orderId}`,
      HttpStatus.PAYMENT_REQUIRED,
    );
  }
}
