import { Inject, Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { UserDb } from '../entity/user.entity';
import { User, UserData } from 'src/user/core/entity/user';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { IUserMapper } from '../mappers/user.mapper.interface';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';
import { IClearableRepository } from 'src/shared/types/clearable';

@Injectable()
export class UserRepository implements IUserRepository, IClearableRepository {
  constructor(
    @InjectRepository(UserDb)
    private readonly userRepository: Repository<UserDb>,
    private readonly userMapper: IUserMapper,
  ) {}

  async save(user: User): Promise<void> {
    //@fix - return user
    const userDb = this.userMapper.toDb(user);
    await this.userRepository.save(userDb);
  }

  async findByEmail(email: UserData['email']): Promise<User | null> {
    const userDb = await this.userRepository.findOne({ where: { email } });
    return userDb ? this.userMapper.toDomain(userDb) : null;
  }

  async findById(id: UserData['id']): Promise<User | null> {
    const userDb = await this.userRepository.findOne({ where: { id } });
    return userDb ? this.userMapper.toDomain(userDb) : null;
  }

  async findAll(): Promise<User[]> {
    const usersDb = await this.userRepository.find();
    return usersDb.map((userDb) => this.userMapper.toDomain(userDb));
  }

  async update(user: User): Promise<User> {
    const updatedUserDb = this.userMapper.toDb(user);
    const result = await this.userRepository.update(user.id, updatedUserDb);

    if (result.affected === 0) {
      throw new UserNotFoundException('id', user.id);
    }

    return user;
  }

  async deleteById(id: UserData['id']): Promise<void> {
    const result = await this.userRepository.delete(id);
    if (result.affected === 0) {
      throw new UserNotFoundException('id', id);
    }
  }

  async clear(): Promise<void> {
    await this.userRepository.query(`DELETE FROM "users"`);
  }
}
