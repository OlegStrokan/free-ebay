// use-cases/get-user-by-email.use-case.ts
import { Inject, Injectable } from '@nestjs/common';
import { UserData } from 'src/user/core/entity/user';
import {
  IUserRepository,
  USER_REPOSITORY,
} from 'src/user/core/repository/user.repository';
import { IGetUserByIdUseCase } from './get-user-by-id.interface';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';

@Injectable()
export class GetUserByIdUseCase implements IGetUserByIdUseCase {
  constructor(
    @Inject(USER_REPOSITORY)
    private readonly userRepository: IUserRepository,
  ) {}

  async execute(userId: UserData['id']) {
    const user = await this.userRepository.findById(userId);
    if (!user) {
      throw new UserNotFoundException('id', userId);
    }
    return user;
  }
}
