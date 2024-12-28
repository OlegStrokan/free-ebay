import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { UserDb } from '../entity/user.entity';

@Injectable()
export class UserRepository {
  constructor(
    @InjectRepository(UserDb)
    private readonly userRepository: Repository<UserDb>,
  ) {}

  async createUser(username: string, password: string): Promise<UserDb> {
    const user = this.userRepository.create({ username, password });
    return await this.userRepository.save(user);
  }

  async findByUsername(username: string): Promise<UserDb | undefined> {
    return this.userRepository.findOne({ where: { username } });
  }

  async findById(id: string): Promise<UserDb | undefined> {
    return this.userRepository.findOne({ where: { id } });
  }
}
