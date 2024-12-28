import { TypeOrmModule } from '@nestjs/typeorm';
import { UserController } from './interface/user.controller';
import { GetUserByEmailUseCase } from './epplication/use-cases/get-user-by-email/get-user-by-email.use-case';
import { GetUserByIdUseCase } from './epplication/use-cases/get-user-by-id/get-user-by-id.use-case';
import { UserDb } from './infrastructure/entity/user.entity';
import { UserRepository } from './infrastructure/repository/user.repository';
import { Module } from '@nestjs/common';

@Module({
  imports: [TypeOrmModule.forFeature([UserDb])],
  providers: [UserRepository, GetUserByEmailUseCase, GetUserByIdUseCase],
  controllers: [UserController],
  exports: [GetUserByEmailUseCase, GetUserByIdUseCase, UserRepository],
})
export class UserModule {}
