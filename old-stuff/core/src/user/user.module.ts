import { TypeOrmModule } from '@nestjs/typeorm';
import { Module } from '@nestjs/common';
import { UserDb } from './infrastructure/entity/user.entity';
import { UserController } from './interface/user.controller';
import { userProviders } from './user.provider';

@Module({
  imports: [TypeOrmModule.forFeature([UserDb])],
  providers: [...userProviders],
  controllers: [UserController],
  exports: [...userProviders],
})
export class UserModule {}
