import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';
import { User, UserData } from '../user';

export interface IUserMockService {
  getOneToCreate(overrides?: Partial<UserData>): CreateUserDto;
  getOne(overrides?: Partial<UserData>): User;
  createOne(overrides?: Partial<UserData>): Promise<void>;
}
