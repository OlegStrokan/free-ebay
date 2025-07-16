import { HttpException, HttpStatus } from '@nestjs/common';

export class PaymentNotFoundException extends HttpException {
  constructor(key: string, value: string) {
    super(`Payment with ${key}: ${value} not found`, HttpStatus.NOT_FOUND);
  }
}
