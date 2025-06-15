import { Injectable } from '@nestjs/common';
import { User } from 'src/user/core/entity/user';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { IGetUsersUseCase } from './get-users.interface';

@Injectable()
export class GetUsersUseCase implements IGetUsersUseCase {
  constructor(private readonly userRepository: IUserRepository) {}

  async execute(): Promise<User[]> {
    return await this.userRepository.findAll();
  }
}
