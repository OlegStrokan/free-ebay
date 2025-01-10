import { Provider } from '@nestjs/common';
import {
  CREATE_USER_USE_CASE,
  DELETE_USER_USE_CASE,
  GET_USER_BY_EMAIL_USE_CASE,
  GET_USER_BY_ID_USE_CASE,
  GET_USERS_USE_CASE,
  UPDATE_USER_USE_CASE,
} from './epplication/injection-tokens/use-case.token';
import { CreateUserUseCase } from './epplication/use-cases/create-user/create-user.use-case';
import { UpdateUserUseCase } from './epplication/use-cases/update-user/update-user.use-case';
import { GetUserByEmailUseCase } from './epplication/use-cases/get-user-by-email/get-user-by-email.use-case';
import { GetUserByIdUseCase } from './epplication/use-cases/get-user-by-id/get-user-by-id.use-case';
import { GetUsersUseCase } from './epplication/use-cases/get-users/get-users.use-case';
import { DeleteUserUseCase } from './epplication/use-cases/delete-user/delete-user.use-case';
import { UserMockService } from './core/entity/mocks/user-mock.service';
import { USER_MOCK_SERVICE } from './epplication/injection-tokens/mock-services.token';
import { UserMapper } from './infrastructure/mappers/user.mapper';
import { USER_MAPPER } from './epplication/injection-tokens/mapper.token';
import { UserRepository } from './infrastructure/repository/user.repository';
import { USER_REPOSITORY } from './epplication/injection-tokens/repository.token';

export const userProvider: Provider[] = [
  {
    useClass: UserMapper,
    provide: USER_MAPPER,
  },
  {
    useClass: UserRepository,
    provide: USER_REPOSITORY,
  },
  {
    useClass: CreateUserUseCase,
    provide: CREATE_USER_USE_CASE,
  },
  {
    useClass: UpdateUserUseCase,
    provide: UPDATE_USER_USE_CASE,
  },
  {
    useClass: GetUserByEmailUseCase,
    provide: GET_USER_BY_EMAIL_USE_CASE,
  },
  {
    useClass: GetUserByIdUseCase,
    provide: GET_USER_BY_ID_USE_CASE,
  },
  {
    useClass: GetUsersUseCase,
    provide: GET_USERS_USE_CASE,
  },
  {
    useClass: DeleteUserUseCase,
    provide: DELETE_USER_USE_CASE,
  },
  {
    useClass: UserMockService,
    provide: USER_MOCK_SERVICE,
  },
];
