import { HttpException, HttpStatus } from '@nestjs/common';

export class ProductAlreadyExistsException extends HttpException {
  constructor(sku: string) {
    super(`Product with SKU ${sku} already exists`, HttpStatus.CONFLICT);
  }
}
