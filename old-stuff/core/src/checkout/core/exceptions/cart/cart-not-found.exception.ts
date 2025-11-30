import { HttpException, HttpStatus } from '@nestjs/common';

export class CartNotFoundException extends HttpException {
  constructor(key: string, value: string) {
    super(`Cart with ${key} ${value} not found`, HttpStatus.NOT_FOUND);
  }
}
