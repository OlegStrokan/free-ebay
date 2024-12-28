import { Injectable } from '@nestjs/common';
import * as bcrypt from 'bcrypt';
import { IRegisterUseCase } from './register.interface';
import { UserRepository } from 'src/auth/infrastructure/repository/user.repository';
import { IUserRepository } from 'src/auth/core/repository/user.repository';
import { RegisterDto } from 'src/auth/interface/dtos/register.dto';

@Injectable()
export class RegisterUseCase implements IRegisterUseCase {
  constructor(private readonly userRepository: IUserRepository) {}

  async execute(dto: RegisterDto): Promise<any> {
    const existingUser = await this.userRepository.findByUsername(dto.username);
    if (existingUser) {
      throw new Error('Username already exists');
    }

    const hashedPassword = await bcrypt.hash(dto.password, 10);
    const user = await this.userRepository.createUser(
      dto.username,
      hashedPassword,
    );

    return { message: 'User registered successfully', userId: user.id };
  }
}
