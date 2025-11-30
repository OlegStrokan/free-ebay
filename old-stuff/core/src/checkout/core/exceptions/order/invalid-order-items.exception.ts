import { HttpException, HttpStatus } from '@nestjs/common';

export class InvalidOrderItemsException extends HttpException {
  constructor() {
    super(
      'One or more items in the order have a quantity of 0',
      HttpStatus.BAD_REQUEST,
    );
  }
}
