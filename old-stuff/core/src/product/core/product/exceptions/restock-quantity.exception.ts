import { HttpException, HttpStatus } from '@nestjs/common';

export class RestockQuantityException extends HttpException {
  constructor() {
    super(`Restock quantity must be positive`, HttpStatus.BAD_GATEWAY);
  }
}
