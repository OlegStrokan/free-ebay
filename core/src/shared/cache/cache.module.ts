import { Module } from '@nestjs/common';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { RedisModule } from '@nestjs-modules/ioredis';
import { ICacheService } from './cache.interface';
import { CacheService } from './cache.service';

@Module({
  imports: [
    RedisModule.forRootAsync({
      imports: [ConfigModule],
      inject: [ConfigService],
      useFactory: async (configService: ConfigService) => ({
        options: {
          host: 'localhost',
          port: 6379,
          password: '${REDIS_PASSWORD}',
        },
        type: 'single',
      }),
    }),
  ],
  providers: [
    {
      provide: ICacheService,
      useClass: CacheService,
    },
  ],
  exports: [ICacheService],
})
export class CacheModule {}
