import { User } from '../entity/user';

export interface IUserRepository {
  save(userData: User): Promise<void>;
  findByEmail(email: string): Promise<User | null>;
  findById(id: string): Promise<User | null>;
  findAll(): Promise<User[]>;
  update(user: User): Promise<User>;
  deleteById(id: string): Promise<void>;
}
