import { UserDb } from 'src/user/infrastructure/entity/user.entity';
import { UserData } from '../entity/user';

export interface IUserRepository {
  save(userData: UserData): Promise<UserDb>;
  findByEmail(email: string): Promise<UserDb | undefined>;
  findById(id: string): Promise<UserDb | undefined>;
}
