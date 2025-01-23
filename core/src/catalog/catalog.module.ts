import { forwardRef, Module } from '@nestjs/common';
import { CatalogController } from './interface/catalog.controller';
import { TypeOrmModule } from '@nestjs/typeorm';
import { CategoryDb } from './infrastructure/entity/category.entity';

import { ProductModule } from 'src/product/product.module';
import { categoryProviders } from './category.provider';

@Module({
  imports: [
    TypeOrmModule.forFeature([CategoryDb]),
    forwardRef(() => ProductModule),
  ],
  controllers: [CatalogController],
  providers: [...categoryProviders],
  exports: [...categoryProviders],
})
export class CatalogModule {}
