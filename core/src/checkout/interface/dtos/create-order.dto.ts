import { IsEnum, IsString } from 'class-validator';
import { PaymentMethod } from 'src/checkout/core/entity/payment/payment';

export class CreateOrderDto {
  @IsString()
  cartId!: string;

  @IsString()
  shippingAddress!: string;

  @IsEnum(PaymentMethod)
  paymentMethod!: PaymentMethod;
}
