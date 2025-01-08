import { Module } from '@nestjs/common';
import { CatalogController } from './interface/catalog.controller';
import { CreateCategoryUseCase } from './epplication/use-cases/create-category/create-category.use-case';
import { DeleteCategoryUseCase } from './epplication/use-cases/delete-category/delete-category.use-case';
import { GetAllCategoriesUseCase } from './epplication/use-cases/get-all-categories/get-all-categories.use-case';
import { UpdateCategoryUseCase } from './epplication/use-cases/update-category/update-category.use-case';
import { CategoryMapper } from './infrastructure/mapper/category.mapper';
import { CategoryRepository } from './infrastructure/repository/category.repository';
import { GetCategoryByIdUseCase } from './epplication/use-cases/get-category-by-id/get-category-by-id.use-case';
import { TypeOrmModule } from '@nestjs/typeorm';
import { CategoryDb } from './infrastructure/entity/category';
import { CategoryMockService } from './core/category/entity/mocks/category-mock.service';
import {
  CATEGORY_MAPPER,
  CATEGORY_REPOSITORY,
} from './core/category/repository/category.repository';
import { ProductModule } from 'src/product/product.module';

@Module({
  imports: [TypeOrmModule.forFeature([CategoryDb]), ProductModule],
  controllers: [CatalogController],
  providers: [
    CategoryRepository,
    CategoryMapper,
    CategoryMockService,
    GetAllCategoriesUseCase,
    GetCategoryByIdUseCase,
    CreateCategoryUseCase,
    UpdateCategoryUseCase,
    DeleteCategoryUseCase,
    {
      provide: CATEGORY_REPOSITORY,
      useClass: CategoryRepository,
    },
    {
      provide: CATEGORY_MAPPER,
      useClass: CategoryMapper,
    },
  ],
})
export class CatalogModule {}
