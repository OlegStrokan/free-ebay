import { ApiProperty } from '@nestjs/swagger';

export class UserDto {
  @ApiProperty({
    description: 'Unique identifier for the user',
    example: '123e4567-e89b-12d3-a456-426614174000',
  })
  id!: string;

  @ApiProperty({
    description: 'Email address of the user',
    example: 'user@example.com',
  })
  email!: string;

  @ApiProperty({
    description: 'Hashed password of the user',
    example: '$2b$10$abc123hashedpassword',
  })
  password!: string;
}
