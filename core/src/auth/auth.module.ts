import { TypeOrmModule } from '@nestjs/typeorm';
import { LoginUseCase } from './epplication/use-cases/login/login.use-case';
import { RegisterUseCase } from './epplication/use-cases/register/register.use-case';
import { Module } from '@nestjs/common';
import { UserRepository } from '../user/infrastructure/repository/user.repository';
import { UserDb } from '../user/infrastructure/entity/user.entity';
import { TokenService } from './epplication/service/token.service';
import { AuthController } from './interface/auth.controller';
import { UserModule } from 'src/user/user.module';

@Module({
  imports: [TypeOrmModule.forFeature([UserDb]), UserModule],
  providers: [UserRepository, RegisterUseCase, LoginUseCase, TokenService],
  controllers: [AuthController],
})
export class AuthModule {}
