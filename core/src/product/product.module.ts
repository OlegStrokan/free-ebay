import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { ProductDb } from './infrastructure/entity/product.entity';
import { ProductRepository } from './infrastructure/repository/product.repository';
import { CreateProductUseCase } from './epplication/use-cases/create-product/create-product.use-case';
import { ProductsController } from './interface/product.controller';
import { FindProductsUseCase } from './epplication/use-cases/find-products/find-products.use-case';
import { MarkAsAvailableUseCase } from './epplication/use-cases/mark-as-available/mark-as-available.use-case';
import { MarkAsOutOfStockUseCase } from './epplication/use-cases/mark-as-out-of-stock/mark-as-out-of-stock.use-case';
import { ProductMockService } from './core/product/entity/mocks/product-mock.service';
import { MoneyMapper } from './infrastructure/mappers/money/money.mapper';
import { ProductMapper } from './infrastructure/mappers/product/product.mapper';

@Module({
  imports: [TypeOrmModule.forFeature([ProductDb])],
  providers: [
    ProductMapper,
    MoneyMapper,
    CreateProductUseCase,
    FindProductsUseCase,
    MarkAsOutOfStockUseCase,
    MarkAsAvailableUseCase,
    ProductRepository,
    ProductMockService,
  ],
  exports: [],
  controllers: [ProductsController],
})
export class ProductModule {}
