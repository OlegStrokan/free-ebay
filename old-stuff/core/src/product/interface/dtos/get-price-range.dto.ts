import { IsString, IsNotEmpty, IsNumber, IsPositive } from 'class-validator';
import { ApiProperty } from '@nestjs/swagger';

export class GetPriceRangeDto {
  @ApiProperty({
    description: 'Product title',
    example: 'iPhone 15 Pro Max',
  })
  @IsString()
  @IsNotEmpty()
  title!: string;

  @ApiProperty({
    description: 'Product description',
    example: 'Latest iPhone with advanced camera system and A17 Pro chip',
  })
  @IsString()
  @IsNotEmpty()
  description!: string;

  @ApiProperty({
    description: 'Product price in cents',
    example: 119900,
  })
  @IsNumber()
  @IsPositive()
  price!: number;
}
