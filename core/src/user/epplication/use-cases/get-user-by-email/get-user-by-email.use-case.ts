import { Inject, Injectable } from '@nestjs/common';
import { UserData } from 'src/user/core/entity/user';
import {
  IUserRepository,
  USER_REPOSITORY,
} from 'src/user/core/repository/user.repository';
import { IGetUserByEmailUseCase } from './get-user-by-email.interface';

@Injectable()
export class GetUserByEmailUseCase implements IGetUserByEmailUseCase {
  constructor(
    @Inject(USER_REPOSITORY)
    private readonly userRepository: IUserRepository,
  ) {}

  async execute(email: UserData['email']) {
    return this.userRepository.findByEmail(email);
  }
}
