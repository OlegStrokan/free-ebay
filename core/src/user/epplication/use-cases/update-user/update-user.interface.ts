import { IUseCase } from 'src/shared/types/use-case.interface';
import { User } from 'src/user/core/entity/user';
import { UpdateUserRequest } from './update-user.use-case';

export type IUpdateUserUseCase = IUseCase<UpdateUserRequest, User>;
