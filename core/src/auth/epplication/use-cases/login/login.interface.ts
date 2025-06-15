import { LoginRequestDto } from 'src/auth/interface/dtos/login-request.dto';
import { User } from 'src/user/core/entity/user';

export type LoginResponseType = {
  user: User;
  accessToken: string;
  refreshToken: string;
};

export abstract class ILoginUseCase {
  abstract execute(dto: LoginRequestDto): Promise<LoginResponseType>;
}
