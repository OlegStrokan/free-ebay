import { Injectable } from '@nestjs/common';
import { IUserMapper } from './user.mapper.interface';
import { User, UserData } from 'src/user/core/entity/user';
import { UserDb } from '../entity/user.entity';

@Injectable()
export class UserMapper implements IUserMapper<UserData, User, UserDb> {
  toDb({ data }: User): UserDb {
    const userDb = new UserDb();
    userDb.id = data.id;
    userDb.createdAt = data.createdAt;
    userDb.updatedAt = data.updatedAt;
    userDb.email = data.email;
    userDb.password = data.password;

    return userDb;
  }
  toDomain(userDb: UserDb): User {
    const userData: UserData = {
      id: userDb.id,
      createdAt: userDb.createdAt,
      updatedAt: userDb.updatedAt,
      email: userDb.email,
      password: userDb.password,
    };
    return new User(userData);
  }

  toClient({ data }: User): UserData {
    return data;
  }
}
