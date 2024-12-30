import { IUseCase } from 'src/shared/types/use-case.interface';
import { User, UserData } from 'src/user/core/entity/user';

export type IGetUserByEmailUseCase = IUseCase<UserData['email'], User>;
