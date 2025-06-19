import { Module } from '@nestjs/common';
import { ProductIndexingService } from './product-indexing.service';
import { ElasticsearchConfigModule } from 'src/tools/elastic-search/elastic-search.module';

@Module({
  imports: [ElasticsearchConfigModule],
  providers: [ProductIndexingService],
  exports: [ProductIndexingService],
})
export class ProductIndexingModule {}
