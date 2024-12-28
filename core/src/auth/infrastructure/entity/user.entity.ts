import { Entity, Column, PrimaryColumn } from 'typeorm';

@Entity()
export class UserDb {
  @PrimaryColumn()
  id: string;

  @Column()
  username: string;

  @Column()
  password: string;
}
