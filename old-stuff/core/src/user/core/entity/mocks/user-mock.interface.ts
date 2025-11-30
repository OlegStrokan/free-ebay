import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';
import { User, UserData } from '../user';

export abstract class IUserMockService {
  abstract getOneToCreate(overrides?: Partial<UserData>): CreateUserDto;
  abstract getOne(overrides?: Partial<UserData>): User;
  abstract createOne(overrides?: Partial<UserData>): Promise<void>;
}
