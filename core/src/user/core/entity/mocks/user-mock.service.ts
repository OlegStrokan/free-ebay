import { Inject, Injectable } from '@nestjs/common';
import { IUserMockService } from './user-mock.interface';
import {
  IUserRepository,
  USER_REPOSITORY,
} from '../../repository/user.repository';
import { User, UserData } from '../user';
import { faker } from '@faker-js/faker';
import { CreateUserDto } from 'src/auth/interface/dtos/register.dto';

@Injectable()
export class UserMockService implements IUserMockService {
  constructor(
    @Inject(USER_REPOSITORY)
    private readonly userRepository: IUserRepository,
  ) {}

  getOneToCreate(): CreateUserDto {
    return {
      email: faker.internet.email(),
      password: faker.internet.password({ length: 8 }),
    };
  }

  getOne(overrides?: Partial<UserData>): User {
    const user = User.create({
      email: overrides.email ?? faker.internet.email(),
      password: overrides.password ?? faker.internet.password({ length: 8 }),
    });

    return user;
  }

  async createOne(overrides: Partial<UserData>): Promise<void> {
    const user = this.getOne(overrides);
    return await this.userRepository.save(user);
  }
}
