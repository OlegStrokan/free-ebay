import { ApiProperty } from '@nestjs/swagger';
import { Money } from 'src/shared/types/money';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { CategoryDto } from 'src/catalog/interface/dtos/category.dto';

export class ProductDto {
  @ApiProperty({
    description: 'Unique identifier of the product',
    example: '123e4567-e89b-12d3-a456-426614174000',
  })
  id!: string;

  @ApiProperty({
    description: 'SKU of the product',
    example: 'SKU12345',
  })
  sku!: string;

  @ApiProperty({
    description: 'Name of the product',
    example: 'Wireless Mouse',
  })
  name!: string;

  @ApiProperty({
    description: 'Description of the product',
    example: 'A high-quality wireless mouse with ergonomic design.',
  })
  description!: string;

  @ApiProperty({
    description: 'Price of the product',
    example: { currency: 'USD', amount: 49.99 },
  })
  price!: Money;

  @ApiProperty({
    description: 'Current status of the product',
    enum: ProductStatus,
    example: ProductStatus.Available,
  })
  status!: ProductStatus;

  @ApiProperty({
    description: 'Available stock quantity',
    example: 100,
  })
  stock!: number;

  @ApiProperty({
    description: 'Timestamp when the product was created',
    example: '2024-01-20T12:00:00Z',
  })
  createdAt!: Date;

  @ApiProperty({
    description: 'Timestamp when the product was last updated',
    example: '2024-01-21T12:00:00Z',
  })
  updatedAt!: Date;

  @ApiProperty({
    description: 'Category of the product (optional)',
    example: { id: 'category123', name: 'Electronics' },
    required: false,
  })
  category?: CategoryDto;

  @ApiProperty({
    description: 'Timestamp when the product was discontinued (optional)',
    example: '2025-01-01T00:00:00Z',
    required: false,
  })
  discontinuedAt?: Date;
}
