import { Entity, Column, PrimaryColumn } from 'typeorm';

@Entity()
export class TokenDb {
  @PrimaryColumn()
  id: string;

  @Column()
  userId: string;

  @Column()
  accessToken: string;

  @Column()
  refreshToken: string;
}
