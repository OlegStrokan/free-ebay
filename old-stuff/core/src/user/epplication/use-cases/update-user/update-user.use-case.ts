import { IUserRepository } from 'src/user/core/repository/user.repository';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';
import { IUpdateUserUseCase, UpdateUserRequest } from './update-user.interface';
import { User } from 'src/user/core/entity/user';
import { Injectable } from '@nestjs/common';

@Injectable()
export class UpdateUserUseCase implements IUpdateUserUseCase {
  constructor(private readonly userRepository: IUserRepository) {}

  public async execute(user: UpdateUserRequest): Promise<User> {
    const existingUser = await this.userRepository.findById(user.id);
    if (!existingUser) {
      throw new UserNotFoundException('id', user.id);
    }

    return await this.userRepository.update(existingUser);
  }
}
