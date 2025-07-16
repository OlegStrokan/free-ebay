import { ApiProperty } from '@nestjs/swagger';
import { PaymentStatus } from 'src/checkout/core/entity/payment/payment';

export class UpdatePaymentStatusDto {
  @ApiProperty({ example: 'pi_123', description: 'Payment Intent ID' })
  paymentIntentId!: string;

  @ApiProperty({ enum: PaymentStatus, description: 'New payment status' })
  newStatus!: PaymentStatus;
}
