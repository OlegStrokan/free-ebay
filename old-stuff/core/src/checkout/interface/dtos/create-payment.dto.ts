import { IsString, IsEnum, ValidateNested } from 'class-validator';
import { Type } from 'class-transformer';
import { PaymentMethod } from 'src/checkout/core/entity/payment/payment';
import { Money } from 'src/shared/types/money';

export class CreatePaymentDto {
  @IsString()
  orderId!: string;

  @IsEnum(PaymentMethod)
  paymentMethod!: PaymentMethod;

  @ValidateNested()
  @Type(() => Money)
  amount!: Money;
}
