import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';

export abstract class IRegisterUseCase {
  abstract execute(dto: CreateUserDto): Promise<void>;
}
