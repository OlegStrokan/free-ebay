import { UserData } from 'src/user/core/entity/user';

export abstract class ITokenService {
  abstract createAccessToken(user: UserData): string;
  abstract createRefreshToken(user: UserData): string;
  abstract verifyAccessToken(token: string): any;
}
