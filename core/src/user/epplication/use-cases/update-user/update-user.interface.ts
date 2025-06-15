import { User } from 'src/user/core/entity/user';
import { UpdateUserDto } from 'src/user/interface/dtos/update-user.dto';

export type UpdateUserRequest = {
  id: string;
  dto: UpdateUserDto;
};

export abstract class IUpdateUserUseCase {
  abstract execute(request: UpdateUserRequest): Promise<User>;
}
