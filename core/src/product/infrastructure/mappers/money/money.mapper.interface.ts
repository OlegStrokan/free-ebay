import { Money } from 'src/shared/types/money';

export interface IMoneyMapper {
  toDb(money: Money): string;
  toDomain(moneyString: string | null): Money | null;
}
