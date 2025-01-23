import { LoginRequestDto } from 'src/auth/interface/dtos/login-request.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';
import { User } from 'src/user/core/entity/user';

export type LoginResponseType = {
  user: User;
  accessToken: string;
  refreshToken: string;
};

export type ILoginUseCase = IUseCase<LoginRequestDto, LoginResponseType>;
