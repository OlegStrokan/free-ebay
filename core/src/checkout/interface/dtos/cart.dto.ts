import { ApiProperty } from '@nestjs/swagger';
import { MoneyDto } from 'src/shared/types/money.dto';

export class CartItemDto {
  @ApiProperty()
  id!: string;

  @ApiProperty()
  productId!: string;

  @ApiProperty()
  cartId!: string;

  @ApiProperty()
  quantity!: number;

  @ApiProperty({ type: MoneyDto })
  price!: MoneyDto;

  @ApiProperty()
  createdAt!: Date;

  @ApiProperty()
  updatedAt!: Date;
}

export class CartDto {
  @ApiProperty()
  id!: string;

  @ApiProperty()
  userId!: string;

  @ApiProperty({ type: [CartItemDto] })
  items!: CartItemDto[];

  @ApiProperty({ type: MoneyDto })
  totalPrice!: MoneyDto;

  @ApiProperty()
  createdAt!: Date;

  @ApiProperty()
  updatedAt!: Date;
}
