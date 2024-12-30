import { CreateUserDto } from 'src/auth/interface/dtos/register.dto';
import { User, UserData } from '../user';

export interface IUserMockService {
  getOneToCreate(): CreateUserDto;
  getOne(overrides?: Partial<UserData>): User;
  createOne(overrides?: Partial<UserData>): Promise<void>;
}
