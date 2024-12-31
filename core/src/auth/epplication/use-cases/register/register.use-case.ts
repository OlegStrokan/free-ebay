import { Inject, Injectable } from '@nestjs/common';
import { CreateUserDto } from 'src/auth/interface/dtos/register.dto';
import { CreateUserUseCase } from 'src/user/epplication/use-cases/create-user/create-user.use-case';
import { ICreateUserUseCase } from 'src/user/epplication/use-cases/create-user/create-user.interface';
import { IRegisterUseCase } from './register.interface';

@Injectable()
export class RegisterUseCase implements IRegisterUseCase {
  constructor(
    @Inject(CreateUserUseCase)
    private readonly createUserUseCase: ICreateUserUseCase,
  ) {}

  async execute(dto: CreateUserDto): Promise<string> {
    return await this.createUserUseCase.execute(dto);
  }
}
