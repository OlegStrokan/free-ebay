import { IUseCase } from 'src/shared/types/use-case.interface';
import { User } from 'src/user/core/entity/user';

export type IGetUsersUseCase = IUseCase<void, User[]>;
