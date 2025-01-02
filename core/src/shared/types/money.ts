/**
 * currency type (EUR, USD etc.) - https://cs.wikipedia.org/wiki/ISO_4217
 */
export type ISO4217 = string;

export const CZK_CURRENCY = 'CZK';
export const EUR_CURRENCY = 'EUR';

export type Currency = ISO4217;

export class Money {
  private amount: number;
  private currency: Currency;
  private fraction: number;

  constructor(amount: number, currency: Currency, fraction: number) {
    this.amount = amount;
    this.currency = currency;
    this.fraction = fraction;
  }

  static getDefaultMoney(): Money {
    return new Money(0, 'USD', 100);
  }

  static zero(currency: Currency = CZK_CURRENCY, fraction = 100): Money {
    return new Money(0, currency, fraction);
  }

  add(other: Money): Money {
    this.ensureSameCurrencyAndFraction(other);
    return new Money(this.amount + other.amount, this.currency, this.fraction);
  }

  subtract(other: Money): Money {
    this.ensureSameCurrencyAndFraction(other);
    return new Money(this.amount - other.amount, this.currency, this.fraction);
  }

  format(): string {
    const baseUnits = this.amount / this.fraction;
    return `${baseUnits.toFixed(2)} ${this.currency}`;
  }

  private ensureSameCurrencyAndFraction(other: Money): void {
    if (this.currency !== other.currency || this.fraction !== other.fraction) {
      throw new Error('Currencies and fractions must match for operations');
    }
  }

  getAmount(): number {
    return this.amount;
  }

  getCurrency(): Currency {
    return this.currency;
  }

  getFraction(): number {
    return this.fraction;
  }
}

export const ZERO_AMOUNT_MONEY: Money = Money.zero();
