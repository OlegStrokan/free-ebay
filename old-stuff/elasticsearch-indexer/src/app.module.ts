import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { ProductIndexingModule } from './product-indexing/product-indexing.module';
import { ElasticsearchConfigModule } from './tools/elastic-search/elastic-search.module';

@Module({
  imports: [
    ConfigModule.forRoot({
      envFilePath: `.${process.env.NODE_ENV}.env`,
      isGlobal: true,
    }),
    ElasticsearchConfigModule,
    ProductIndexingModule,
  ],
  providers: [],
})
export class AppModule {}
