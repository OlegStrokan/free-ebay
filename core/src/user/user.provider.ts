import { Provider } from '@nestjs/common';
import { CreateUserUseCase } from './epplication/use-cases/create-user/create-user.use-case';
import { UpdateUserUseCase } from './epplication/use-cases/update-user/update-user.use-case';
import { GetUserByEmailUseCase } from './epplication/use-cases/get-user-by-email/get-user-by-email.use-case';
import { GetUserByIdUseCase } from './epplication/use-cases/get-user-by-id/get-user-by-id.use-case';
import { GetUsersUseCase } from './epplication/use-cases/get-users/get-users.use-case';
import { DeleteUserUseCase } from './epplication/use-cases/delete-user/delete-user.use-case';
import { UserMockService } from './core/entity/mocks/user-mock.service';
import { UserMapper } from './infrastructure/mappers/user.mapper';
import { UserRepository } from './infrastructure/repository/user.repository';
import { IUserRepository } from './core/repository/user.repository';
import { IUserMapper } from './infrastructure/mappers/user.mapper.interface';
import { IUserMockService } from './core/entity/mocks/user-mock.interface';
import { ICreateUserUseCase } from './epplication/use-cases/create-user/create-user.interface';
import { IDeleteUserUseCase } from './epplication/use-cases/delete-user/delete-user.interface';
import { IGetUserByEmailUseCase } from './epplication/use-cases/get-user-by-email/get-user-by-email.interface';
import { IGetUserByIdUseCase } from './epplication/use-cases/get-user-by-id/get-user-by-id.interface';
import { IGetUsersUseCase } from './epplication/use-cases/get-users/get-users.interface';
import { IUpdateUserUseCase } from './epplication/use-cases/update-user/update-user.interface';

export const userProviders: Provider[] = [
  {
    provide: IUserRepository,
    useClass: UserRepository,
  },
  {
    provide: IUserMapper,
    useClass: UserMapper,
  },
  {
    provide: IUserMockService,
    useClass: UserMockService,
  },
  {
    provide: ICreateUserUseCase,
    useClass: CreateUserUseCase,
  },
  {
    provide: IUpdateUserUseCase,
    useClass: UpdateUserUseCase,
  },
  {
    provide: IGetUserByEmailUseCase,
    useClass: GetUserByEmailUseCase,
  },
  {
    provide: IGetUserByIdUseCase,
    useClass: GetUserByIdUseCase,
  },
  {
    provide: IGetUsersUseCase,
    useClass: GetUsersUseCase,
  },
  {
    provide: IDeleteUserUseCase,
    useClass: DeleteUserUseCase,
  },
];
