import { BaseEntity } from 'src/shared/database/base.entity';
import { Entity, Column, PrimaryColumn } from 'typeorm';

@Entity('users')
export class UserDb extends BaseEntity {
  @PrimaryColumn()
  id: string;

  @Column()
  email: string;

  @Column()
  password: string;
}