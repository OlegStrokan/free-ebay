import { Injectable, UnauthorizedException } from '@nestjs/common';
import * as jwt from 'jsonwebtoken';
import { ICacheService } from 'src/shared/cache/cache.interface';
import { UserData } from 'src/user/core/entity/user';

@Injectable()
export class TokenService {
  constructor(private readonly cacheService: ICacheService) {}

  async createAccessToken(user: UserData): Promise<string> {
    return jwt.sign({ id: user.id, email: user.email }, 'access_secret', {
      expiresIn: '15m',
    });
  }

  async createRefreshToken(user: UserData): Promise<string> {
    const token = jwt.sign({ id: user.id }, 'refresh_secret', {
      expiresIn: '7d',
    });

    await this.cacheService.getOrSet(
      `refresh:${user.id}`,
      60 * 60 * 24 * 7,
      async () => token,
    );

    return token;
  }

  async verifyRefreshToken(userId: string, token: string): Promise<boolean> {
    const storedToken = await this.cacheService.getOrSet(
      `refresh:${userId}`,
      60 * 60 * 24 * 7,
      async () => {
        throw new UnauthorizedException('Refresh token not found');
      },
    );

    if (storedToken !== token) {
      throw new UnauthorizedException('Invalid or expired refresh token');
    }

    return true;
  }

  async revokeRefreshToken(userId: string): Promise<void> {
    await this.cacheService.invalidate(`refresh:${userId}`);
  }

  verifyAccessToken(token: string): any {
    try {
      return jwt.verify(token, 'access_secret');
    } catch {
      throw new UnauthorizedException('Invalid access token');
    }
  }
}
