import { Inject, Injectable } from '@nestjs/common';
import { ICreateUserUseCase } from 'src/user/epplication/use-cases/create-user/create-user.interface';
import { IRegisterUseCase } from './register.interface';
import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';
import { CREATE_USER_USE_CASE } from 'src/user/epplication/injection-tokens/use-case.token';

@Injectable()
export class RegisterUseCase implements IRegisterUseCase {
  constructor(
    @Inject(CREATE_USER_USE_CASE)
    private readonly createUserUseCase: ICreateUserUseCase,
  ) {}

  async execute(dto: CreateUserDto): Promise<string> {
    return await this.createUserUseCase.execute(dto);
  }
}
