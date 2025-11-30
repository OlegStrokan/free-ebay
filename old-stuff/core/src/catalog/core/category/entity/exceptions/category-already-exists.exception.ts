import { HttpException, HttpStatus } from '@nestjs/common';

export class CategoryAlreadyExistsException extends HttpException {
  constructor(key: string, id: string) {
    super(`Category with ${key} ${id} already exists`, HttpStatus.CONFLICT);
  }
}
