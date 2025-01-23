import { forwardRef, Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { productProvider } from './product.provider';
import { ProductsController } from './interface/product.controller';
import { ProductDb } from './infrastructure/entity/product.entity';
import { AuthModule } from 'src/auth/auth.module';
import { CatalogModule } from 'src/catalog/catalog.module';

@Module({
  imports: [
    TypeOrmModule.forFeature([ProductDb]),
    AuthModule,
    forwardRef(() => CatalogModule),
  ],
  providers: [...productProvider],
  exports: [...productProvider],
  controllers: [ProductsController],
})
export class ProductModule {}
