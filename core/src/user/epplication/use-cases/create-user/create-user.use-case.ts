import { Inject } from '@nestjs/common';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { UserRepository } from 'src/user/infrastructure/repository/user.repository';
import { ICreateUserUseCase } from './create-user.interface';
import { CreateUserDto } from 'src/auth/interface/dtos/register.dto';
import * as bcrypt from 'bcrypt';
import { User } from 'src/user/core/entity/user';

export class CreateUserUseCase implements ICreateUserUseCase {
  constructor(
    @Inject(UserRepository)
    private readonly userRepository: IUserRepository,
  ) {}

  public async execute(dto: CreateUserDto): Promise<string> {
    const existingUser = await this.userRepository.findByEmail(dto.email);
    if (existingUser) {
      throw new Error('User already exists');
    }

    const hashedPassword = await bcrypt.hash(dto.password, 10);
    const user = User.create({ ...dto, password: hashedPassword });
    await this.userRepository.save(user.data);
    return 'OK';
  }
}
