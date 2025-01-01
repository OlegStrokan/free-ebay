import { User } from '../entity/user';

export const USER_REPOSITORY = Symbol('USER_REPOSITORY');
export const USER_MAPPER = Symbol('USER_MAPPER');

export interface IUserRepository {
  save(userData: User): Promise<void>;
  findByEmail(email: string): Promise<User | null>;
  findById(id: string): Promise<User | null>;
  findAll(): Promise<User[]>;
  updateById(user: User): Promise<User>;
  deleteById(id: string): Promise<void>;
}
