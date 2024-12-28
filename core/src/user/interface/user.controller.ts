import { Controller, Get, Inject, Query } from '@nestjs/common';
import { IGetUserByIdUseCase } from '../epplication/use-cases/get-user-by-id/get-user-by-id.interface';
import { GetUserByIdUseCase } from '../epplication/use-cases/get-user-by-id/get-user-by-id.use-case';

@Controller('user')
export class UserController {
  constructor(
    @Inject(GetUserByIdUseCase)
    private readonly getUserByIdUseCase: IGetUserByIdUseCase,
  ) {}

  @Get(':id')
  async getUserById(@Query('id') id: string) {
    return this.getUserByIdUseCase.execute(id);
  }
}
