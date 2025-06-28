import { Provider } from '@nestjs/common';
import { ProductMapper } from './infrastructure/mappers/product/product.mapper';
import { ProductRepository } from './infrastructure/repository/product.repository';
import { ProductMockService } from './core/product/entity/mocks/product-mock.service';
import { CreateProductUseCase } from './epplication/use-cases/create-product/create-product.use-case';
import { DeleteProductUseCase } from './epplication/use-cases/delete-product/delete-product.use-case';
import { FindProductUseCase } from './epplication/use-cases/find-product/find-product.use-case';
import { MarkAsAvailableUseCase } from './epplication/use-cases/mark-as-available/mark-as-available.use-case';
import { MarkAsOutOfStockUseCase } from './epplication/use-cases/mark-as-out-of-stock/mark-as-out-of-stock.use-case';
import { FindProductsUseCase } from './epplication/use-cases/find-products/find-products.use-case';
import { MoneyMapper } from './infrastructure/mappers/money/money.mapper';
import { IProductRepository } from './core/product/repository/product.repository';
import { IProductMapper } from './infrastructure/mappers/product/product.mapper.interface';
import { IProductMockService } from './core/product/entity/mocks/product-mock.interface';
import { ICreateProductUseCase } from './epplication/use-cases/create-product/create-product.interface';
import { IDeleteProductUseCase } from './epplication/use-cases/delete-product/delete-product.interface';
import { IFindProductUseCase } from './epplication/use-cases/find-product/find-product.interface';
import { IFindProductsUseCase } from './epplication/use-cases/find-products/find-product.interface';
import { IMarkAsAvailableUseCase } from './epplication/use-cases/mark-as-available/mark-as-available.interface';
import { IMarkAsOutOfStockUseCase } from './epplication/use-cases/mark-as-out-of-stock/mark-as-out-of-stock.interface';
import { IMoneyMapper } from './infrastructure/mappers/money/money.mapper.interface';
import { ICacheService } from 'src/shared/cache/cache.interface';
import { CacheService } from 'src/shared/cache/cache.service';
import { SearchProductsUseCase } from './epplication/use-cases/search-products/search-products.use-case';
import { ISearchProductsUseCase } from './epplication/use-cases/search-products/search-products.interface';
import { GetPriceRangeUseCase } from './epplication/use-cases/get-price-range/get-price-range.use-case';
import { IGetPriceRangeUseCase } from './epplication/use-cases/get-price-range/get-price-range.interface';

export const productProviders: Provider[] = [
  {
    provide: IProductRepository,
    useClass: ProductRepository,
  },
  {
    provide: ICacheService,
    useClass: CacheService,
  },
  {
    provide: IProductMapper,
    useClass: ProductMapper,
  },
  {
    provide: IMoneyMapper,
    useClass: MoneyMapper,
  },
  {
    provide: IProductMockService,
    useClass: ProductMockService,
  },
  {
    provide: ICreateProductUseCase,
    useClass: CreateProductUseCase,
  },
  {
    provide: IDeleteProductUseCase,
    useClass: DeleteProductUseCase,
  },
  {
    provide: IFindProductUseCase,
    useClass: FindProductUseCase,
  },
  {
    provide: ISearchProductsUseCase,
    useClass: SearchProductsUseCase,
  },
  {
    provide: IFindProductsUseCase,
    useClass: FindProductsUseCase,
  },
  {
    provide: IMarkAsAvailableUseCase,
    useClass: MarkAsAvailableUseCase,
  },
  {
    provide: IMarkAsOutOfStockUseCase,
    useClass: MarkAsOutOfStockUseCase,
  },
  {
    provide: IGetPriceRangeUseCase,
    useClass: GetPriceRangeUseCase,
  },
];
