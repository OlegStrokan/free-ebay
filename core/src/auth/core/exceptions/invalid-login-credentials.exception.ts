import { HttpException, HttpStatus } from '@nestjs/common';

export class InvalidLoginCredentialsException extends HttpException {
  constructor() {
    super(`Invalid email or password`, HttpStatus.UNAUTHORIZED);
  }
}
