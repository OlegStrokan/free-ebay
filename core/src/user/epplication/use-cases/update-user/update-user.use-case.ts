import { Inject } from '@nestjs/common';
import {
  IUserRepository,
  USER_REPOSITORY,
} from 'src/user/core/repository/user.repository';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';
import { IUpdateUserUseCase } from './update-user.interface';
import { User } from 'src/user/core/entity/user';
import { UpdateUserDto } from 'src/user/interface/dtos/update-user.dto';

// @Todo....maybe i should refactor this shit in the future
export type UpdateUserRequest = {
  id: string;
  dto: UpdateUserDto;
};

export class UpdateUserUseCase implements IUpdateUserUseCase {
  constructor(
    @Inject(USER_REPOSITORY)
    private readonly userRepository: IUserRepository,
  ) {}

  public async execute(user: UpdateUserRequest): Promise<User> {
    const existingUser = await this.userRepository.findById(user.id);
    if (!existingUser) {
      throw new UserNotFoundException('id', user.id);
    }

    return await this.userRepository.update(existingUser);
  }
}
