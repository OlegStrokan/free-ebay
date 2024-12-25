import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { ProductDb } from './infrastructure/entity/product.entity';
import { ProductRepository } from './infrastructure/repository/product.repository';
import { CreateProductUseCase } from './epplication/use-cases/create-product.use-cases';

@Module({
  imports: [TypeOrmModule.forFeature([ProductDb])],
  providers: [CreateProductUseCase, ProductRepository],
  exports: [CreateProductUseCase],
})
export class ProductModule {}
