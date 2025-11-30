import { Injectable } from '@nestjs/common';
import { ICreateUserUseCase } from 'src/user/epplication/use-cases/create-user/create-user.interface';
import { IRegisterUseCase } from './register.interface';
import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';

@Injectable()
export class RegisterUseCase implements IRegisterUseCase {
  constructor(private readonly createUserUseCase: ICreateUserUseCase) {}

  async execute(dto: CreateUserDto): Promise<void> {
    return await this.createUserUseCase.execute(dto);
  }
}
