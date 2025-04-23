import { ApiProperty } from '@nestjs/swagger';

export type ISO4217 = string;

export const CZK_CURRENCY = 'CZK';
export const EUR_CURRENCY = 'EUR';

export type Currency = ISO4217;

export class MoneyDto {
  @ApiProperty({
    description: 'The amount of money in smallest units (e.g., cents for USD)',
    example: 10000,
  })
  amount!: number;

  @ApiProperty({
    description: 'The currency of the money (ISO 4217 format)',
    example: 'CZK',
    enum: [CZK_CURRENCY, EUR_CURRENCY],
  })
  currency!: Currency;

  @ApiProperty({
    description:
      'The fractional part of the money (e.g., cents if the currency has two decimal places)',
    example: 2,
  })
  fraction!: number;
}
