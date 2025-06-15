import { Provider } from '@nestjs/common';
import { CategoryMapper } from './infrastructure/mapper/category.mapper';
import { CategoryMockService } from './core/category/entity/mocks/category-mock.service';
import { CategoryRepository } from './infrastructure/repository/category.repository';
import { CreateCategoryUseCase } from './epplication/use-cases/create-category/create-category.use-case';
import { UpdateCategoryUseCase } from './epplication/use-cases/update-category/update-category.use-case';
import { GetAllCategoriesUseCase } from './epplication/use-cases/get-all-categories/get-all-categories.use-case';
import { GetCategoryByIdUseCase } from './epplication/use-cases/get-category-by-id/get-category-by-id.use-case';
import { DeleteCategoryUseCase } from './epplication/use-cases/delete-category/delete-category.use-case';
import { ICategoryRepository } from './core/category/repository/category.repository';
import { ICategoryMapper } from './infrastructure/mapper/category.mapper.interface';
import { ICreateCategoryUseCase } from './epplication/use-cases/create-category/create-category.interface';
import { IUpdateCategoryUseCase } from './epplication/use-cases/update-category/update-category.interface';
import { IGetAllCategoriesUseCase } from './epplication/use-cases/get-all-categories/get-all-categories.interface';
import { IGetCategoryByIdUseCase } from './epplication/use-cases/get-category-by-id/get-category-by-id.interface';
import { IDeleteCategoryUseCase } from './epplication/use-cases/delete-category/delete-category.interface';

export const categoryProviders: Provider[] = [
  {
    provide: ICategoryRepository,
    useClass: CategoryRepository,
  },
  {
    provide: ICategoryMapper,
    useClass: CategoryMapper,
  },
  {
    provide: CategoryMockService,
    useClass: CategoryMockService,
  },
  {
    provide: ICreateCategoryUseCase,
    useClass: CreateCategoryUseCase,
  },
  {
    provide: IUpdateCategoryUseCase,
    useClass: UpdateCategoryUseCase,
  },
  {
    provide: IGetAllCategoriesUseCase,
    useClass: GetAllCategoriesUseCase,
  },
  {
    provide: IGetCategoryByIdUseCase,
    useClass: GetCategoryByIdUseCase,
  },
  {
    provide: IDeleteCategoryUseCase,
    useClass: DeleteCategoryUseCase,
  },
];
