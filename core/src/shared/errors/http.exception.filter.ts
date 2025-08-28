import {
  ExceptionFilter,
  Catch,
  ArgumentsHost,
  HttpException,
} from '@nestjs/common';

@Catch(HttpException)
export class HttpExceptionFilter implements ExceptionFilter {
  catch(exception: HttpException, host: ArgumentsHost) {
    const ctx = host.switchToHttp();
    const response = ctx.getResponse();
    const status = exception.getStatus();
    const exceptionResponse = exception.getResponse();

    // If the response is an object (like validation errors), use it directly
    if (typeof exceptionResponse === 'object') {
      response.status(status).json(exceptionResponse);
    } else {
      response.status(status).json({
        statusCode: status,
        message: exceptionResponse,
      });
    }
  }
}
