import { HttpException, HttpStatus } from '@nestjs/common';

export class AiAgentServerException extends HttpException {
  constructor() {
    super(
      `AI agent currently unavailable. Please, try later`,
      HttpStatus.SERVICE_UNAVAILABLE,
    );
  }
}
