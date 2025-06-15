import { User, UserData } from 'src/user/core/entity/user';

export abstract class IGetUserByIdUseCase {
  abstract execute(id: UserData['id']): Promise<User>;
}
