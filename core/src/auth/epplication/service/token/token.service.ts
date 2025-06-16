// token.service.ts
import { Injectable, UnauthorizedException } from '@nestjs/common';
import { InjectRedis } from '@nestjs-modules/ioredis';
import * as jwt from 'jsonwebtoken';
import { UserData } from 'src/user/core/entity/user';
import Redis from 'ioredis';

@Injectable()
export class TokenService {
  constructor(@InjectRedis() private readonly redis: Redis) {}

  async createAccessToken(user: UserData): Promise<string> {
    return jwt.sign({ id: user.id, email: user.email }, 'access_secret', {
      expiresIn: '15m',
    });
  }

  async createRefreshToken(user: UserData): Promise<string> {
    const token = jwt.sign({ id: user.id }, 'refresh_secret', {
      expiresIn: '7d',
    });

    await this.redis.set(`refresh:${user.id}`, token, 'EX', 60 * 60 * 24 * 7);

    return token;
  }

  verifyAccessToken(token: string): any {
    try {
      return jwt.verify(token, 'access_secret');
    } catch (error) {
      throw new UnauthorizedException('Invalid access token');
    }
  }

  async verifyRefreshToken(userId: string, token: string): Promise<boolean> {
    const stored = await this.redis.get(`refresh:${userId}`);
    if (!stored || stored !== token) {
      throw new UnauthorizedException('Invalid or expired refresh token');
    }
    return true;
  }

  async revokeRefreshToken(userId: string): Promise<void> {
    await this.redis.del(`refresh:${userId}`);
  }
}
