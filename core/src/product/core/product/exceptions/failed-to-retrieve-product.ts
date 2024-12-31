import { HttpException, HttpStatus } from '@nestjs/common';

export class FailedToRetrieveProductException extends HttpException {
  constructor(id: string) {
    super(
      `Failed to retrieve the saved product with id ${id}`,
      HttpStatus.INTERNAL_SERVER_ERROR,
    );
  }
}
