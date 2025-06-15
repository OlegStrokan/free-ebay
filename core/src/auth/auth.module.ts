import { TypeOrmModule } from '@nestjs/typeorm';
import { Module } from '@nestjs/common';
import { UserDb } from '../user/infrastructure/entity/user.entity';
import { AuthController } from './interface/auth.controller';
import { UserModule } from 'src/user/user.module';
import { authProviders } from './auth.providers';

@Module({
  imports: [TypeOrmModule.forFeature([UserDb]), UserModule],
  providers: [...authProviders],
  controllers: [AuthController],
  exports: [...authProviders],
})
export class AuthModule {}
