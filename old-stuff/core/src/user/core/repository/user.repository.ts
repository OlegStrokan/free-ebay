import { User } from '../entity/user';

export abstract class IUserRepository {
  abstract save(userData: User): Promise<void>;
  abstract findByEmail(email: string): Promise<User | null>;
  abstract findById(id: string): Promise<User | null>;
  abstract findAll(): Promise<User[]>;
  abstract update(user: User): Promise<User>;
  abstract deleteById(id: string): Promise<void>;
}
