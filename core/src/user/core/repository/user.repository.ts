import { User } from '../entity/user';

export const USER_REPOSITORY = Symbol('USER_REPOSITORY');
export const USER_MAPPER = Symbol('USER_MAPPER');

export interface IUserRepository {
  save(userData: User): Promise<void>;
  findByEmail(email: string): Promise<User>;
  findById(id: string): Promise<User>;
}
