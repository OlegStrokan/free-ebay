import { HttpException, HttpStatus } from '@nestjs/common';

export class UserNotFoundException extends HttpException {
  constructor(key: string, id: string) {
    super(`User with ${key} ${id} not found`, HttpStatus.NOT_FOUND);
  }
}
