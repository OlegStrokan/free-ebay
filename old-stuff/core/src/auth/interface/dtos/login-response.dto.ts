import { ApiProperty } from '@nestjs/swagger';
import { UserDto } from 'src/user/interface/dtos/user.dto';

export class LoginResponseDto {
  @ApiProperty({ description: 'Details of the logged-in user', type: UserDto })
  user!: UserDto;

  @ApiProperty({
    description: 'JWT access token',
    example: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...',
  })
  accessToken!: string;

  @ApiProperty({
    description: 'JWT refresh token',
    example: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...',
  })
  refreshToken!: string;
}
