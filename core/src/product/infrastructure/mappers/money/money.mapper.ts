import { Money } from 'src/shared/types/money';

import { Injectable } from '@nestjs/common';
import { IMoneyMapper } from './money.mapper.interface';

@Injectable()
export class MoneyMapper implements IMoneyMapper {
  toDb(money: Money): string {
    return JSON.stringify(money);
  }

  toDomain(moneyString: string | null): Money | null {
    return moneyString ? (JSON.parse(moneyString) as Money) : null;
  }
}
