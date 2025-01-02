import { IsUUID } from 'class-validator';
import { IsNotEmpty } from 'class-validator';

export class CreateCartDto {
  @IsUUID()
  @IsNotEmpty()
  userId!: string;
}
