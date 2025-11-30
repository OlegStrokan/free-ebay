import { HttpException, HttpStatus } from '@nestjs/common';

export class ProductNotFoundException extends HttpException {
  constructor(key: string, id: string) {
    super(`Product with ${key} ${id} not found`, HttpStatus.NOT_FOUND);
  }
}
