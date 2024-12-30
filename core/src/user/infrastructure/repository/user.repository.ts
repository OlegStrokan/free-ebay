import { Inject, Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { UserDb } from '../entity/user.entity';
import { User, UserData } from 'src/user/core/entity/user';
import {
  IUserRepository,
  USER_MAPPER,
} from 'src/user/core/repository/user.repository';
import { IUserMapper } from '../mappers/user.mapper.interface';

@Injectable()
export class UserRepository implements IUserRepository {
  constructor(
    @InjectRepository(UserDb)
    private readonly userRepository: Repository<UserDb>,
    @Inject(USER_MAPPER)
    private readonly userMapper: IUserMapper<UserData, User, UserDb>,
  ) {}

  async save(user: User): Promise<void> {
    const userDb = this.userMapper.toDb(user);
    await this.userRepository.save(userDb);
  }

  async findByEmail(email: UserData['email']): Promise<User> {
    const userDb = await this.userRepository.findOne({ where: { email } });
    if (userDb) {
      return this.userMapper.toDomain(userDb);
    }
  }

  async findById(id: UserData['id']): Promise<User> {
    const userDb = await this.userRepository.findOne({ where: { id } });
    return this.userMapper.toDomain(userDb);
  }
}
