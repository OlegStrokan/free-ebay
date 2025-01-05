import { HttpException, HttpStatus } from '@nestjs/common';

export class CartItemNotFoundException extends HttpException {
  constructor(key: string, value: string) {
    super(`Cart item with ${key} ${value} not found`, HttpStatus.NOT_FOUND);
  }
}
