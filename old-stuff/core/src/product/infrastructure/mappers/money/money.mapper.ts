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
    if (!moneyString) return null;
    const obj = JSON.parse(moneyString);
    return new Money(obj.amount, obj.currency, obj.fraction);
  }

  toClient(money: Money): MoneyDto {
    return {
      amount: money.getAmount(),
      currency: money.getCurrency(),
      fraction: money.getFraction(),
    };
  }
}
