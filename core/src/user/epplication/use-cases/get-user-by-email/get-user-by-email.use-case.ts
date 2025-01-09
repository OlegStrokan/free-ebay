import { Inject, Injectable } from '@nestjs/common';
import { User, UserData } from 'src/user/core/entity/user';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { IGetUserByEmailUseCase } from './get-user-by-email.interface';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';
import { USER_REPOSITORY } from '../../injection-tokens/repository.token';

@Injectable()
export class GetUserByEmailUseCase implements IGetUserByEmailUseCase {
  constructor(
    @Inject(USER_REPOSITORY)
    private readonly userRepository: IUserRepository,
  ) {}

  async execute(email: UserData['email']): Promise<User> {
    const user = await this.userRepository.findByEmail(email);
    if (!user) {
      throw new UserNotFoundException('email', email);
    }
    return user;
  }
}
