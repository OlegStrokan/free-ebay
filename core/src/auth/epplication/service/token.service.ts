import { Injectable, UnauthorizedException } from '@nestjs/common';
import * as jwt from 'jsonwebtoken';
import { ITokenService } from './token.service.interface';
import { UserData } from 'src/user/core/entity/user';

@Injectable()
export class TokenService implements ITokenService {
  createAccessToken(user: UserData): string {
    return jwt.sign({ id: user.id, email: user.email }, 'access_secret', {
      expiresIn: '15m',
    });
  }

  createRefreshToken(user: UserData): string {
    return jwt.sign({ id: user.id }, 'refresh_secret', { expiresIn: '7d' });
  }

  verifyAccessToken(token: string): any {
    try {
      return jwt.verify(token, 'access_secret');
    } catch (error) {
      throw new UnauthorizedException('Invalid access token');
    }
  }
}
