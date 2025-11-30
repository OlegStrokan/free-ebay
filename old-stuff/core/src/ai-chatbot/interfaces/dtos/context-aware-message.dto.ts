import { Type } from 'class-transformer';
import { IsString, IsNotEmpty, IsArray, ValidateNested } from 'class-validator';

export class ContextAwareMessageDto {
  @IsString()
  @IsNotEmpty()
  role!: string;

  @IsString()
  @IsNotEmpty()
  content!: string;
}

export class ContextAwareMessagesDto {
  @IsArray()
  @ValidateNested({ each: true })
  @Type(() => ContextAwareMessageDto)
  messages!: ContextAwareMessageDto[];
}
