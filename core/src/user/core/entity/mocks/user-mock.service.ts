import { Inject, Injectable } from '@nestjs/common';
import { IUserMockService } from './user-mock.interface';
import { IUserRepository } from '../../repository/user.repository';
import { User, UserData } from '../user';
import { faker } from '@faker-js/faker';
import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { USER_REPOSITORY } from 'src/user/epplication/injection-tokens/repository.token';

@Injectable()
export class UserMockService implements IUserMockService {
  constructor(
    @Inject(USER_REPOSITORY)
    private readonly userRepository: IUserRepository,
  ) {}

  getOneToCreate(overrides?: Partial<UserData>): CreateUserDto {
    return {
      email: overrides?.email ?? faker.internet.email(),
      password: overrides?.password ?? faker.internet.password({ length: 8 }),
    };
  }

  getOne(overrides?: Partial<UserData>): User {
    const user = new User({
      email: overrides?.email ?? faker.internet.email(),
      password: overrides?.password ?? faker.internet.password({ length: 8 }),
      id: overrides?.id ?? generateUlid(),
      createdAt: overrides?.createdAt ?? new Date(),
      updatedAt: overrides?.updatedAt ?? new Date(),
    });

    return user;
  }

  async createOne(overrides: Partial<UserData>): Promise<void> {
    const user = this.getOne(overrides);
    return await this.userRepository.save(user);
  }
}
