import { HttpException, HttpStatus } from '@nestjs/common';

export class CategoryNotFoundException extends HttpException {
  constructor(key: string, id: string) {
    super(`Category with ${key} ${id} not found`, HttpStatus.NOT_FOUND);
  }
}
