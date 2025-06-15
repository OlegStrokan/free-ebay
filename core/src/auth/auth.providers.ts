import { Provider } from '@nestjs/common';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { UserRepository } from 'src/user/infrastructure/repository/user.repository';
import { TokenService } from './epplication/service/token.service';
import { ITokenService } from './epplication/service/token.service.interface';
import { ILoginUseCase } from './epplication/use-cases/login/login.interface';
import { LoginUseCase } from './epplication/use-cases/login/login.use-case';
import { RegisterUseCase } from './epplication/use-cases/register/register.use-case';
import { IRegisterUseCase } from './epplication/use-cases/register/register.interface';

export const authProviders: Provider[] = [
  {
    provide: IUserRepository,
    useClass: UserRepository,
  },
  {
    provide: ILoginUseCase,
    useClass: LoginUseCase,
  },
  {
    provide: IRegisterUseCase,
    useClass: RegisterUseCase,
  },
  {
    provide: ITokenService,
    useClass: TokenService,
  },
];
