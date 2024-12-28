import { TypeOrmModule } from '@nestjs/typeorm';
import { AccessTokenUseCase } from './epplication/use-cases/access-token/access-token.use-case';
import { LoginUseCase } from './epplication/use-cases/login/login.use-case';
import { RefreshTokenUseCase } from './epplication/use-cases/refresh-token/refresh-token.use-case';
import { RegisterUseCase } from './epplication/use-cases/register/register.use-case';
import { Module } from '@nestjs/common';
import { TokenRepository } from './infrastructure/repository/token.repository';
import { UserRepository } from './infrastructure/repository/user.repository';
import { UserDb } from './infrastructure/entity/user.entity';
import { TokenDb } from './infrastructure/entity/token.entity';

@Module({
  imports: [TypeOrmModule.forFeature([UserDb, TokenDb])],
  providers: [
    UserRepository,
    TokenRepository,
    RegisterUseCase,
    LoginUseCase,
    AccessTokenUseCase,
    RefreshTokenUseCase,
  ],
})
export class AuthModule {}
