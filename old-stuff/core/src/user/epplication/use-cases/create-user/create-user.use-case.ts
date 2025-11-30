import { IUserRepository } from 'src/user/core/repository/user.repository';
import { ICreateUserUseCase } from './create-user.interface';
import * as bcrypt from 'bcrypt';
import { User } from 'src/user/core/entity/user';
import { UserAlreadyExistsException } from 'src/user/core/exceptions/user-already-exists';
import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';
import { Injectable } from '@nestjs/common';

@Injectable()
export class CreateUserUseCase implements ICreateUserUseCase {
  constructor(private readonly userRepository: IUserRepository) {}

  public async execute(dto: CreateUserDto): Promise<void> {
    const existingUser = await this.userRepository.findByEmail(dto.email);
    if (existingUser) {
      throw new UserAlreadyExistsException(dto.email);
    }

    const hashedPassword = await bcrypt.hash(dto.password, 10);
    const user = User.create({ ...dto, password: hashedPassword });
    return await this.userRepository.save(user);
  }
}
