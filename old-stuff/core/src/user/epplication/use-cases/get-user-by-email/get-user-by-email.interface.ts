import { User, UserData } from 'src/user/core/entity/user';

export abstract class IGetUserByEmailUseCase {
  abstract execute(email: UserData['email']): Promise<User>;
}
