import { Provider } from '@nestjs/common';
import { CATEGORY_MAPPER } from './epplication/injection-tokens/mapper.token';
import { CategoryMapper } from './infrastructure/mapper/category.mapper';
import { CATEGORY_MOCK_SERVICE } from './epplication/injection-tokens/mock-services.token';
import { CATEGORY_REPOSITORY } from './epplication/injection-tokens/repository.token';
import {
  CREATE_CATEGORY_USE_CASE,
  DELETE_CATEGORY_USE_CASE,
  GET_ALL_CATEGORIES_USE_CASE,
  GET_CATEGORY_BY_ID_USE_CASE,
  UPDATE_CATEGORY_USE_CASE,
} from './epplication/injection-tokens/use-case.token';
import { CategoryMockService } from './core/category/entity/mocks/category-mock.service';
import { CategoryRepository } from './infrastructure/repository/category.repository';
import { CreateCategoryUseCase } from './epplication/use-cases/create-category/create-category.use-case';
import { UpdateCategoryUseCase } from './epplication/use-cases/update-category/update-category.use-case';
import { GetAllCategoriesUseCase } from './epplication/use-cases/get-all-categories/get-all-categories.use-case';
import { GetCategoryByIdUseCase } from './epplication/use-cases/get-category-by-id/get-category-by-id.use-case';
import { DeleteCategoryUseCase } from './epplication/use-cases/delete-category/delete-category.use-case';

export const categoryProviders: Provider[] = [
  {
    provide: CATEGORY_MAPPER,
    useClass: CategoryMapper,
  },
  {
    provide: CATEGORY_MOCK_SERVICE,
    useClass: CategoryMockService,
  },
  {
    provide: CATEGORY_REPOSITORY,
    useClass: CategoryRepository,
  },
  {
    provide: CREATE_CATEGORY_USE_CASE,
    useClass: CreateCategoryUseCase,
  },
  {
    provide: UPDATE_CATEGORY_USE_CASE,
    useClass: UpdateCategoryUseCase,
  },
  {
    provide: GET_ALL_CATEGORIES_USE_CASE,
    useClass: GetAllCategoriesUseCase,
  },
  {
    provide: GET_CATEGORY_BY_ID_USE_CASE,
    useClass: GetCategoryByIdUseCase,
  },
  {
    provide: DELETE_CATEGORY_USE_CASE,
    useClass: DeleteCategoryUseCase,
  },
];
