import { Inject, Injectable } from '@nestjs/common';
import { UserData } from 'src/user/core/entity/user';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { IGetUserByIdUseCase } from './get-user-by-id.interface';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';
import { USER_REPOSITORY } from '../../injection-tokens/repository.token';

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
