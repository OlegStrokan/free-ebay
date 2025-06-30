import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { APP_INTERCEPTOR } from '@nestjs/core';
import { AiChatBotModule } from './ai-chatbot/ai-chatbot.module';
import { AuthModule } from './auth/auth.module';
import { CatalogModule } from './catalog/catalog.module';
import { CheckoutModule } from './checkout/checkout.module';
import { ProductModule } from './product/product.module';
import { CacheModule } from './shared/cache/cache.module';
import { MetricsInterceptor } from './shared/interceptors/metrics.interceptor';
import { KafkaModule } from './shared/kafka/kafka.module';
import { UserModule } from './user/user.module';
import { DatabaseModule } from './shared/database/database.module';
import { ElasticsearchConfigModule } from './shared/elastic-search/elastic-search.module';
import { PromptBuilderModule } from './shared/prompt-builder/prompt-builder.module';

@Module({
  imports: [
    ConfigModule.forRoot({
      envFilePath: `.${process.env.NODE_ENV}.env`,
      isGlobal: true,
    }),
    ElasticsearchConfigModule,
    ProductModule,
    AuthModule,
    UserModule,
    CatalogModule,
    CheckoutModule,
    DatabaseModule,
    AiChatBotModule,
    KafkaModule,
    CacheModule,
    PromptBuilderModule,
  ],
  providers: [
    {
      provide: APP_INTERCEPTOR,
      useClass: MetricsInterceptor,
    },
  ],
})
export class AppModule {}
