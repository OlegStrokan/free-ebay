export abstract class ICacheService {
  abstract getOrSet<T>(
    key: string,
    ttl: number,
    fetcher: () => Promise<T>,
  ): Promise<T>;

  abstract set<T>(key: string, ttl: number, value: T): Promise<void>;

  abstract invalidate(key: string): Promise<void>;
}
