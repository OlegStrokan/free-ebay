import { MoneyMapper } from './money.mapper';
import { Money } from 'src/shared/types/money';
import { MoneyDto } from 'src/shared/types/money.dto';

describe('MoneyMapper', () => {
  let moneyMapper: MoneyMapper;

  beforeAll(() => {
    moneyMapper = new MoneyMapper();
  });

  it('should convert Money to DB string and back (toDb <-> toDomain)', () => {
    const money = new Money(1000, 'USD', 100);
    const dbString = moneyMapper.toDb(money);
    expect(typeof dbString).toBe('string');

    const fromDb = moneyMapper.toDomain(dbString);
    expect(fromDb).not.toBeNull();
    if (fromDb) {
      expect(fromDb.getAmount()).toBe(money.getAmount());
      expect(fromDb.getCurrency()).toBe(money.getCurrency());
      expect(fromDb.getFraction()).toBe(money.getFraction());
    }
  });

  it('should return null from toDomain if input is null', () => {
    expect(moneyMapper.toDomain(null)).toBeNull();
  });

  it('should convert Money to MoneyDto (toClient)', () => {
    const money = new Money(2500, 'EUR', 100);
    const dto: MoneyDto = moneyMapper.toClient(money);
    expect(dto).toEqual({
      amount: 2500,
      currency: 'EUR',
      fraction: 100,
    });
  });
});
