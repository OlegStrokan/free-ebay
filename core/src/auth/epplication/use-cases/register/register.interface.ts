import { RegisterDto } from 'src/auth/interface/dtos/register.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IRegisterUseCase = IUseCase<RegisterDto, any>;
