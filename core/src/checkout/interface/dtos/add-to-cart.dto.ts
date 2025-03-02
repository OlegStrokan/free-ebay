import { IsString, IsNumber, Min } from 'class-validator';

export class AddToCartDto {
  @IsString()
  cartId!: string;

  @IsString()
  productId!: string;

  @IsNumber()
  @Min(1)
  quantity!: number;
}
