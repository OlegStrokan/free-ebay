import { HttpException, HttpStatus } from '@nestjs/common';

export class PaymentNotFoundException extends HttpException {
  constructor(id: string) {
    super(`Payment with ${id} not found`, HttpStatus.NOT_FOUND);
  }
}
