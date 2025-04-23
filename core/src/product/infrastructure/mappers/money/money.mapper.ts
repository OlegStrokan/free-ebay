import { Money } from 'src/shared/types/money';

import { Injectable } from '@nestjs/common';
import { IMoneyMapper } from './money.mapper.interface';
import { MoneyDto } from 'src/shared/types/money.dto';

@Injectable()
export class MoneyMapper implements IMoneyMapper {
  toDb(money: Money): string {
    return JSON.stringify(money);
  }

  toDomain(moneyString: string | null): Money | null {
    return moneyString ? (JSON.parse(moneyString) as Money) : null;
  }

  toClient(money: Money): MoneyDto {
    return {
      amount: money.getAmount(),
      currency: money.getCurrency(),
      fraction: money.getFraction(),
    };
  }
}
