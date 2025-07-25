import { forwardRef, Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { ProductsController } from './interface/product.controller';
import { ProductDb } from './infrastructure/entity/product.entity';
import { AuthModule } from 'src/auth/auth.module';
import { CatalogModule } from 'src/catalog/catalog.module';
import { productProviders } from './product.provider';
import { KafkaModule } from 'src/shared/kafka/kafka.module';
import { ElasticsearchConfigModule } from 'src/shared/elastic-search/elastic-search.module';
import { AiChatBotModule } from 'src/ai-chatbot/ai-chatbot.module';
import { PromptBuilderModule } from 'src/shared/prompt-builder/prompt-builder.module';

@Module({
  imports: [
    ElasticsearchConfigModule,
    TypeOrmModule.forFeature([ProductDb]),
    AuthModule,
    KafkaModule,
    AiChatBotModule,
    PromptBuilderModule,
    forwardRef(() => CatalogModule),
  ],
  providers: [...productProviders],
  exports: [...productProviders],
  controllers: [ProductsController],
})
export class ProductModule {}
