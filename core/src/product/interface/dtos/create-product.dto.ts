import { IsString, IsNotEmpty, IsOptional, IsEnum } from 'class-validator';
import { ApiProperty } from '@nestjs/swagger';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { Money } from 'src/shared/types/money';

export class CreateProductDto {
  @ApiProperty({
    description: 'SKU of the product',
    example: 'SKU12345',
  })
  @IsString()
  @IsNotEmpty()
  sku!: string;

  @ApiProperty({
    description: 'Name of the product',
    example: 'Wireless Mouse',
  })
  @IsString()
  @IsNotEmpty()
  name!: string;

  @ApiProperty({
    description: 'Description of the product',
    example: 'A high-quality wireless mouse with ergonomic design.',
  })
  @IsString()
  @IsNotEmpty()
  description!: string;

  @ApiProperty({
    description: 'Status of the product',
    enum: ProductStatus,
    example: ProductStatus.Available,
    required: false,
  })
  @IsEnum(ProductStatus)
  @IsOptional()
  status?: ProductStatus = ProductStatus.Available;

  @ApiProperty({
    description: 'Price of the product',
    example: { currency: 'USD', amount: 49, fraction: 1 },
  })
  @IsNotEmpty()
  price!: Money;
}
