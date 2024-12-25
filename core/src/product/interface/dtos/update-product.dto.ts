import {
  IsString,
  IsOptional,
  IsEnum,
  IsDate,
  IsNumber,
} from 'class-validator';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { Money } from 'src/shared/types/money';

export class UpdateProductDto {
  @IsEnum(ProductStatus)
  @IsOptional()
  status?: ProductStatus;

  @IsNumber()
  @IsOptional()
  price?: Money;

  @IsString()
  @IsOptional()
  name?: string;

  @IsString()
  @IsOptional()
  description?: string;

  @IsString()
  @IsOptional()
  category?: string;

  @IsDate()
  @IsOptional()
  discontinuedAt?: Date;
}
