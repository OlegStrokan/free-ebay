import { UserData } from 'src/user/core/entity/user';

export abstract class IUserService {
  abstract getUserById(id: string): Promise<UserData>;
  abstract getUserByEmail(email: string): Promise<UserData>;
  abstract createUser(user: UserData): Promise<UserData>;
  abstract updateUser(id: string, user: Partial<UserData>): Promise<UserData>;
  abstract deleteUser(id: string): Promise<void>;
}
