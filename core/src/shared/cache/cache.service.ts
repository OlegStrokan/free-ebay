import { InjectRedis } from '@nestjs-modules/ioredis';
import { Injectable } from '@nestjs/common';
import Redis from 'ioredis';
import { ICacheService } from './cache.interface';

@Injectable()
export class CacheService implements ICacheService {
  constructor(@InjectRedis() private readonly redis: Redis) {}

  async getOrSet<T>(
    key: string,
    ttl: number,
    fetcher: () => Promise<T>,
  ): Promise<T> {
    const cached = await this.redis.get(key);
    if (cached) return JSON.parse(cached) as T;

    const value = await fetcher();
    await this.redis.set(key, JSON.stringify(value), 'EX', ttl);
    return value;
  }

  async set<T>(key: string, ttl: number, value: T): Promise<void> {
    await this.redis.set(key, JSON.stringify(value), 'EX', ttl);
  }

  async invalidate(key: string): Promise<void> {
    await this.redis.del(key);
  }
}
