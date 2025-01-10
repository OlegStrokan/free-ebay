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

export const categoryProviders: Provider[] = [
  {
    provide: CATEGORY_MAPPER,
    useClass: CategoryMapper,
  },
  {
    provide: CATEGORY_MOCK_SERVICE,
    useClass: CategoryMapper,
  },
  {
    provide: CATEGORY_REPOSITORY,
    useClass: CategoryMapper,
  },
  {
    provide: CREATE_CATEGORY_USE_CASE,
    useClass: CategoryMapper,
  },
  {
    provide: UPDATE_CATEGORY_USE_CASE,
    useClass: CategoryMapper,
  },
  {
    provide: GET_ALL_CATEGORIES_USE_CASE,
    useClass: CategoryMapper,
  },
  {
    provide: GET_CATEGORY_BY_ID_USE_CASE,
    useClass: CategoryMapper,
  },
  {
    provide: DELETE_CATEGORY_USE_CASE,
    useClass: CategoryMapper,
  },
  {
    provide: CATEGORY_MAPPER,
    useClass: CategoryMapper,
  },
];
