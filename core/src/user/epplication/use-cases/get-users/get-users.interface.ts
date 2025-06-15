import { User } from 'src/user/core/entity/user';

export abstract class IGetUsersUseCase {
  abstract execute(): Promise<User[]>;
}
