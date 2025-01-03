import { HttpException, HttpStatus } from '@nestjs/common';

export class CartAlreadyExists extends HttpException {
  constructor(userId: string) {
    super(
      `Cart already exists for user with id: ${userId}`,
      HttpStatus.CONFLICT,
    );
  }
}
