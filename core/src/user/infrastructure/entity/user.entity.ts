import { CartDb } from 'src/checkout/infrastructure/entity/card.entity';
import { OrderDb } from 'src/checkout/infrastructure/entity/order.entity';
import { PaymentDb } from 'src/checkout/infrastructure/entity/payment.entity';
import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import {
  Entity,
  Column,
  PrimaryColumn,
  JoinColumn,
  OneToMany,
  OneToOne,
} from 'typeorm';

@Entity('users')
export class UserDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @Column()
  email!: string;

  @Column()
  password!: string;

  @OneToOne(() => CartDb, (cart) => cart.user, { onDelete: 'CASCADE' })
  @JoinColumn({ name: 'cart_id' })
  cart!: CartDb;

  @OneToMany(() => OrderDb, (order) => order.user)
  orders!: OrderDb[];

  @OneToMany(() => PaymentDb, (payment) => payment.user)
  payments!: PaymentDb[];
}
