export abstract class ICacheService {
  abstract getOrSet<T>(
    key: string,
    ttl: number,
    fetcher: () => Promise<T>,
  ): Promise<T>;

  abstract invalidate(key: string): Promise<void>;
}
