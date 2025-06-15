import { Money } from 'src/shared/types/money';
import { MoneyDto } from 'src/shared/types/money.dto';

export abstract class IMoneyMapper {
  abstract toDb(money: Money): string;
  abstract toDomain(moneyString: string | null): Money | null;
  abstract toClient(money: Money): MoneyDto;
}
