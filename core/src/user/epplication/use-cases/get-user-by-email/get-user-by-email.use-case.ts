import { Inject, Injectable } from '@nestjs/common';
import { UserData } from 'src/user/core/entity/user';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { IGetUserByEmailUseCase } from './get-user-by-email.interface';
import { UserRepository } from 'src/user/infrastructure/repository/user.repository';

@Injectable()
export class GetUserByEmailUseCase implements IGetUserByEmailUseCase {
  constructor(
    @Inject(UserRepository)
    private readonly userRepository: IUserRepository,
  ) {}

  async execute(email: UserData['email']) {
    return this.userRepository.findByEmail(email);
  }
}
