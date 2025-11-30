import { ApiProperty } from '@nestjs/swagger';
import { ProductDto } from 'src/product/interface/dtos/product.dto';

export class CategoryDto {
  @ApiProperty({
    description: 'Unique identifier of the category',
    example: '123e4567-e89b-12d3-a456-426614174000',
  })
  id!: string;

  @ApiProperty({
    description: 'Name of the category',
    example: 'Electronics',
  })
  name!: string;

  @ApiProperty({
    description: 'Description of the category',
    example: 'Category for electronic products and gadgets.',
  })
  description!: string;

  @ApiProperty({
    description: 'Unique identifier of the parent category (optional)',
    example: '456e7890-e12b-34d5-c678-901234567890',
    required: false,
  })
  parentCategoryId?: string;

  @ApiProperty({
    description: 'Subcategories of this category',
    type: [CategoryDto],
    example: [
      {
        id: 'subcategory123',
        name: 'Mobile Phones',
        description: 'Category for mobile phones.',
        parentCategoryId: '123e4567-e89b-12d3-a456-426614174000',
        children: [],
        products: [],
      },
    ],
  })
  children!: CategoryDto[];

  @ApiProperty({
    description: 'Products belonging to this category',
    type: [ProductDto],
    example: [
      {
        id: 'product123',
        sku: 'SKU123',
        name: 'Smartphone',
        description: 'A high-end smartphone.',
        price: { currency: 'USD', amount: 699 },
        status: 'Available',
        stock: 50,
        createdAt: '2024-01-20T12:00:00Z',
        updatedAt: '2024-01-21T12:00:00Z',
        category: { id: 'category123', name: 'Electronics' },
        discontinuedAt: null,
      },
    ],
  })
  products!: ProductDto[];
}
