import { IUseCase } from 'src/shared/types/use-case.interface';
import { CreateUserDto } from 'src/user/interface/dtos/create-user.dto';

export type ICreateUserUseCase = IUseCase<CreateUserDto, string>;
