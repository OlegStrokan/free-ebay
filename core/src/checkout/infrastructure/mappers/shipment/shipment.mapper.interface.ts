export interface IShipmentMapper<TData, TDomain, TDatabase> {
  toDb(domain: TData): TDatabase;
  toDomain(db: TDatabase): TDomain;
  toClient(domain: TDomain): TData;
}
