import { HttpException, HttpStatus } from '@nestjs/common';

export class CartItemsNotFoundException extends HttpException {
  constructor(value: string) {
    super(
      `Cart with id ${value} doesn't have any items for order creating`,
      HttpStatus.NOT_FOUND,
    );
  }
}
