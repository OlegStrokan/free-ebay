// use-cases/get-user-by-email.use-case.ts
import { Inject, Injectable } from '@nestjs/common';
import { UserData } from 'src/user/core/entity/user';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { IGetUserByIdUseCase } from './get-user-by-id.interface';
import { UserRepository } from 'src/user/infrastructure/repository/user.repository';

@Injectable()
export class GetUserByIdUseCase implements IGetUserByIdUseCase {
  constructor(
    @Inject(UserRepository)
    private readonly userRepository: IUserRepository,
  ) {}

  async execute(userId: UserData['id']) {
    return this.userRepository.findById(userId);
  }
}
