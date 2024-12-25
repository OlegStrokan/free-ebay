/**
 * currency type (EUR, USD etc.) - https://cs.wikipedia.org/wiki/ISO_4217
 */
export type ISO4217 = string;

export const CZK_CURRENCY = 'CZK';
export const EUR_CURRENCY = 'EUR';

export interface Money {
  /**
   * Has to be divided by the fraction to get the base currency units
   */
  amount: number;
  currency: Currency;
  /**
   * A number by which we should divide the amount to get it in the Currency base units
   *
   * e.g. for CZK ... fraction of 100 and amount of 1000 would mean that we are representing the amount in "haléře" and the result would be: 1000 / 100 = 10 Kč
   */
  fraction: number;
}

export interface CzkMoney extends Money {
  currency: typeof CZK_CURRENCY;
  fraction: 100;
}

export type MoneyFormat = Pick<Money, 'currency' | 'fraction'>;

export type Currency = ISO4217;

export const ZERO_AMOUNT_MONEY: Money = Object.freeze({
  amount: 0,
  currency: CZK_CURRENCY,
  fraction: 100,
});
