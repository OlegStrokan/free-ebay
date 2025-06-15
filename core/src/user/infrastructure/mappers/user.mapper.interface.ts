import { User } from 'src/user/core/entity/user';
import { UserDb } from '../entity/user.entity';
import { UserData } from 'src/user/core/entity/user';

export abstract class IUserMapper {
  abstract toDb(domain: User): UserDb;
  abstract toDomain(db: UserDb): User;
  abstract toClient(domain: User): UserData;
}
