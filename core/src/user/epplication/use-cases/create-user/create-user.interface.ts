import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';

export abstract class ICreateUserUseCase {
  abstract execute(dto: CreateUserDto): Promise<string>;
}
