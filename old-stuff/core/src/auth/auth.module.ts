import { TypeOrmModule } from '@nestjs/typeorm';
import { Module } from '@nestjs/common';
import { UserDb } from '../user/infrastructure/entity/user.entity';
import { AuthController } from './interface/auth.controller';
import { UserModule } from 'src/user/user.module';
import { authProviders } from './auth.providers';
import { CacheModule } from 'src/shared/cache/cache.module';

@Module({
  imports: [TypeOrmModule.forFeature([UserDb]), UserModule, CacheModule],
  providers: [...authProviders],
  controllers: [AuthController],
  exports: [...authProviders],
})
export class AuthModule {}
