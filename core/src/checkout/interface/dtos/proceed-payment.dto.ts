import { ApiProperty } from '@nestjs/swagger';
import { PaymentMethod } from 'src/checkout/core/entity/payment/payment';
import { Money } from 'src/shared/types/money';

export class ProceedPaymentDto {
  @ApiProperty({ example: 'order123', description: 'Order ID' })
  orderId!: string;

  @ApiProperty({ enum: PaymentMethod, description: 'Payment method' })
  paymentMethod!: PaymentMethod;

  @ApiProperty({ type: Money, description: 'Payment amount' })
  amount!: Money;

  @ApiProperty({ example: '123 Main St', description: 'Shipping address' })
  shippingAddress!: string;
}
