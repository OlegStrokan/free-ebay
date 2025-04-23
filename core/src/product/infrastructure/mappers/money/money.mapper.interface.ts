import { Money } from 'src/shared/types/money';
import { MoneyDto } from 'src/shared/types/money.dto';

export interface IMoneyMapper {
  toDb(money: Money): string;
  toClient(money: Money): MoneyDto;
  toDomain(moneyString: string | null): Money | null;
}
