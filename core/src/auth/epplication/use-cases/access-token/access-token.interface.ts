import { TokenDto } from 'src/auth/interface/dtos/token.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IAccessTokenUseCase = IUseCase<TokenDto, any>;
