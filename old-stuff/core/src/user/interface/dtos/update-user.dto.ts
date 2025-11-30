import { IsEmail } from 'class-validator';
import { ApiProperty } from '@nestjs/swagger';

export class UpdateUserDto {
  @ApiProperty({
    description: 'Updated user email address',
    example: 'newemail@example.com',
  })
  @IsEmail()
  email!: string;
}
