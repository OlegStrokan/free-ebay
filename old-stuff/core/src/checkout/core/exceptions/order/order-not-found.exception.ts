import { HttpException, HttpStatus } from '@nestjs/common';

export class OrderNotFoundException extends HttpException {
  constructor(id: string) {
    super(`Order with ${id} not found`, HttpStatus.NOT_FOUND);
  }
}
