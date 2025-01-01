import { Inject, Injectable } from '@nestjs/common';
import { CreateUserUseCase } from 'src/user/epplication/use-cases/create-user/create-user.use-case';
import { ICreateUserUseCase } from 'src/user/epplication/use-cases/create-user/create-user.interface';
import { IRegisterUseCase } from './register.interface';
import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';

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
