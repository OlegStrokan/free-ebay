import { Provider } from '@nestjs/common';
import { ProductMapper } from './infrastructure/mappers/product/product.mapper';
import {
  MONEY_MAPPER,
  PRODUCT_MAPPER,
} from './epplication/injection-tokens/mapper.token';
import { ProductRepository } from './infrastructure/repository/product.repository';
import { ProductMockService } from './core/product/entity/mocks/product-mock.service';
import { PRODUCT_MOCK_SERVICE } from './epplication/injection-tokens/mock-services.token';
import { CreateProductUseCase } from './epplication/use-cases/create-product/create-product.use-case';
import { DeleteProductUseCase } from './epplication/use-cases/delete-product/delete-product.use-case';
import {
  CREATE_PRODUCT_USE_CASE,
  DELETE_PRODUCT_USE_CASE,
  FIND_PRODUCT_USE_CASE,
  FIND_PRODUCTS_USE_CASE,
  MARK_AS_AVAILABLE_USE_CASE,
  MARK_AS_OUT_OF_STOCK_USE_CASE,
} from './epplication/injection-tokens/use-case.token';
import { FindProductUseCase } from './epplication/use-cases/find-product/find-product.use-case';
import { MarkAsAvailableUseCase } from './epplication/use-cases/mark-as-available/mark-as-available.use-case';
import { MarkAsOutOfStockUseCase } from './epplication/use-cases/mark-as-out-of-stock/mark-as-out-of-stock.use-case';
import { FindProductsUseCase } from './epplication/use-cases/find-products/find-products.use-case';
import { MoneyMapper } from './infrastructure/mappers/money/money.mapper';
import { PRODUCT_REPOSITORY } from './epplication/injection-tokens/repository.token';

export const productProvider: Provider[] = [
  {
    useClass: ProductMapper,
    provide: PRODUCT_MAPPER,
  },
  {
    useClass: MoneyMapper,
    provide: MONEY_MAPPER,
  },
  {
    useClass: ProductRepository,
    provide: PRODUCT_REPOSITORY,
  },
  {
    useClass: ProductRepository,
    provide: PRODUCT_MAPPER,
  },
  {
    useClass: ProductMockService,
    provide: PRODUCT_MOCK_SERVICE,
  },
  {
    useClass: ProductMapper,
    provide: PRODUCT_MAPPER,
  },
  {
    useClass: CreateProductUseCase,
    provide: CREATE_PRODUCT_USE_CASE,
  },
  {
    useClass: DeleteProductUseCase,
    provide: DELETE_PRODUCT_USE_CASE,
  },
  {
    useClass: FindProductUseCase,
    provide: FIND_PRODUCT_USE_CASE,
  },
  {
    useClass: FindProductsUseCase,
    provide: FIND_PRODUCTS_USE_CASE,
  },
  {
    useClass: MarkAsAvailableUseCase,
    provide: MARK_AS_AVAILABLE_USE_CASE,
  },
  {
    useClass: MarkAsOutOfStockUseCase,
    provide: MARK_AS_OUT_OF_STOCK_USE_CASE,
  },
];
