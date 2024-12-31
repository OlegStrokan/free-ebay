import { HttpException, HttpStatus } from '@nestjs/common';

export class ProductNotFoundException extends HttpException {
  constructor(id: string) {
    super(`Product with id ${id} not found`, HttpStatus.NOT_FOUND);
  }
}
