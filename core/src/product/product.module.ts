import { forwardRef, Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { ProductsController } from './interface/product.controller';
import { ProductDb } from './infrastructure/entity/product.entity';
import { AuthModule } from 'src/auth/auth.module';
import { CatalogModule } from 'src/catalog/catalog.module';
import { productProviders } from './product.provider';

@Module({
  imports: [
    TypeOrmModule.forFeature([ProductDb]),
    AuthModule,
    forwardRef(() => CatalogModule),
  ],
  providers: [...productProviders],
  exports: [...productProviders],
  controllers: [ProductsController],
})
export class ProductModule {}
