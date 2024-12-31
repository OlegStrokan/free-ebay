import { UserData } from 'src/user/core/entity/user';

export interface ITokenService {
  createAccessToken(user: UserData): string;
  createRefreshToken(user: UserData): string;
  verifyAccessToken(token: string): any;
}
