import { LoginDto } from 'src/auth/interface/dtos/login.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type ILoginUseCase = IUseCase<LoginDto, any>;
