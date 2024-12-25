import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { ProductDb } from './infrastructure/entity/product.entity';
import { ProductRepository } from './infrastructure/repository/product.repository';
import { CreateProductUseCase } from './epplication/use-cases/create-product.use-case';
import { ProductsController } from './interface/product.controller';
import { FindProductsUseCase } from './epplication/use-cases/find-products.use-case';

@Module({
  imports: [TypeOrmModule.forFeature([ProductDb])],
  providers: [CreateProductUseCase, FindProductsUseCase, ProductRepository],
  exports: [CreateProductUseCase, FindProductsUseCase],
  controllers: [ProductsController],
})
export class ProductModule {}
