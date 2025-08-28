import { ApiProperty } from '@nestjs/swagger';
import { IsEnum, IsString, ValidateNested } from 'class-validator';
import { Type } from 'class-transformer';
import { PaymentMethod } from 'src/checkout/core/entity/payment/payment';
import { MoneyDto } from 'src/shared/types/money.dto';

export class ProceedPaymentDto {
  @ApiProperty()
  @IsString()
  orderId!: string;

  @ApiProperty({ enum: PaymentMethod })
  @IsEnum(PaymentMethod)
  paymentMethod!: PaymentMethod;

  @ApiProperty({ type: MoneyDto })
  @ValidateNested()
  @Type(() => MoneyDto)
  amount!: MoneyDto;

  @ApiProperty()
  @IsString()
  shippingAddress!: string;
}
