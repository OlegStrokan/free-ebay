import { CreateUserDto } from 'src/auth/interface/dtos/register.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type ICreateUserUseCase = IUseCase<CreateUserDto, string>;
