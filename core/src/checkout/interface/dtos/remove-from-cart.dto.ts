import { IsString } from 'class-validator';

export class RemoveFromCartDto {
  @IsString()
  cartId!: string;

  @IsString()
  cartItemId!: string;
}
