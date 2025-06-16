import { IsNotEmpty, IsString } from 'class-validator';

export class UserMessageDto {
  @IsNotEmpty()
  @IsString()
  query!: string;
}
