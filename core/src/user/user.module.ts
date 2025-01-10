import { TypeOrmModule } from '@nestjs/typeorm';
import { Module } from '@nestjs/common';
import { UserDb } from './infrastructure/entity/user.entity';
import { userProvider } from './user.provider';
import { UserController } from './interface/user.controller';

console.log(userProvider);
@Module({
  imports: [TypeOrmModule.forFeature([UserDb])],
  providers: [...userProvider],
  controllers: [UserController],
  exports: [...userProvider],
})
export class UserModule {}
