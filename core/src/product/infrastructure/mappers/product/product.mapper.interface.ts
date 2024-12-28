export interface IProductMapper<TClient, TDomain, TDatabase> {
  toDb(domain: TDomain): TDatabase;
  toDomain(db: TDatabase): TDomain;
  toClient(domain: TDomain): TClient;
}
