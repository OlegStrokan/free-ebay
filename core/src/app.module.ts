import { Module } from '@nestjs/common';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { Client } from '@elastic/elasticsearch';
import { ProductModule } from './product/product.module';
import { AuthModule } from './auth/auth.module';
import { UserModule } from './user/user.module';
import { CatalogModule } from './catalog/catalog.module';
import { CheckoutModule } from './checkout/checkout.module';
import { MetricsInterceptor } from './shared/interceptors/metrics.interceptor';
import { APP_INTERCEPTOR } from '@nestjs/core';
import { DatabaseModule } from './shared/database/database.module';
import { AiChatBotModule } from './ai-chatbot/ai-chatbot.module';
import { RedisModule } from '@nestjs-modules/ioredis';

@Module({
  imports: [
    ConfigModule.forRoot({
      envFilePath: `.${process.env.NODE_ENV}.env`,
      isGlobal: true,
    }),
    RedisModule.forRootAsync({
      imports: [ConfigModule],
      inject: [ConfigService],
      useFactory: async (configService: ConfigService) => ({
        options: {
          host: configService.get<string>('REDIS_HOST', 'localhost'),
          port: configService.get<number>('REDIS_PORT', 6379),
          password: configService.get<string>('REDIS_PASSWORD'),
        },
        type: 'single',
      }),
    }),
    ProductModule,
    AuthModule,
    UserModule,
    CatalogModule,
    CheckoutModule,
    DatabaseModule,
    AiChatBotModule,
  ],
  providers: [
    {
      provide: APP_INTERCEPTOR,
      useClass: MetricsInterceptor,
    },
    {
      provide: 'ELASTIC_CLIENT',
      inject: [ConfigService],
      useFactory: (configService: ConfigService) => {
        return new Client({
          node: configService.get<string>(
            'ELASTIC_NODE',
            'http://localhost:9200',
          ),
          auth: {
            username: configService.get<string>('ELASTIC_USERNAME')!,
            password: configService.get<string>('ELASTIC_PASSWORD')!,
          },
        });
      },
    },
  ],
  exports: ['ELASTIC_CLIENT'],
})
export class AppModule {}
