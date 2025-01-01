import {
  IsNotEmpty,
  IsUUID,
  ValidateNested,
  IsOptional,
  IsArray,
} from 'class-validator';
import { Type } from 'class-transformer';
import { AddToCartDto } from './add-to-cart.dto';

export class CreateCartDto {
  @IsUUID()
  @IsNotEmpty()
  userId!: string;

  @IsOptional()
  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => AddToCartDto)
  items?: AddToCartDto[];
}
