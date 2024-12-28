import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { UserDb } from '../entity/user.entity';
import { UserData } from 'src/user/core/entity/user';
import { IUserRepository } from 'src/user/core/repository/user.repository';

@Injectable()
export class UserRepository implements IUserRepository {
  constructor(
    @InjectRepository(UserDb)
    private readonly userRepository: Repository<UserDb>,
  ) {}

  async save(user: UserData): Promise<UserDb> {
    return await this.userRepository.save(user);
  }

  async findByEmail(email: UserData['email']): Promise<UserDb | undefined> {
    return this.userRepository.findOne({ where: { email } });
  }

  async findById(id: UserData['id']): Promise<UserDb | undefined> {
    return this.userRepository.findOne({ where: { id } });
  }
}
