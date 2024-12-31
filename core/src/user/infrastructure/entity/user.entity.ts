import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { Entity, Column, PrimaryColumn } from 'typeorm';

@Entity('users')
export class UserDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @Column()
  email!: string;

  @Column()
  password!: string;
}
