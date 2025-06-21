import { Module } from '@nestjs/common';
import { ProductIndexingService } from './product-indexing.service';
import { ElasticsearchConfigModule } from 'src/tools/elastic-search/elastic-search.module';
import { ProductIndexingController } from './product-indexing.controller';

@Module({
  imports: [ElasticsearchConfigModule],
  providers: [ProductIndexingService],
  controllers: [ProductIndexingController],
  exports: [ProductIndexingService],
})
export class ProductIndexingModule {}
