import { IUseCase } from 'src/shared/types/use-case.interface';
import { UserData } from 'src/user/core/entity/user';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';

export type IGetUserByIdUseCase = IUseCase<UserData['id'], UserDb>;
