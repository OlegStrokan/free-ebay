export interface IProductMapper<TGraphQL, TDomain, TDatabase> {
  toDb(domain: TDomain): TDatabase;
  toDomain(db: TDatabase): TDomain;
  toClient(domain: TDomain): TGraphQL;
}
