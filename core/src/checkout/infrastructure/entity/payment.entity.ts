import {
  Entity,
  Column,
  PrimaryColumn,
  OneToOne,
  ManyToOne,
  JoinColumn,
  OneToMany,
} from 'typeorm';
import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { OrderDb } from './order.entity';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';
import { PaymentStatusDb } from './payment-status.entity';

@Entity('payments')
export class PaymentDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @OneToOne(() => OrderDb, (order) => order.payment, { onDelete: 'CASCADE' })
  @JoinColumn({ name: 'order_id' })
  order!: OrderDb;

  @ManyToOne(() => UserDb, (user) => user.payments, { onDelete: 'CASCADE' })
  @JoinColumn({ name: 'user_id' })
  user!: UserDb;

  @Column({ type: 'varchar', length: 50 })
  paymentStatus!: string;

  @Column({ type: 'decimal' })
  amount!: number;

  @Column({ type: 'timestamp', default: () => 'CURRENT_TIMESTAMP' })
  paymentDate!: Date;

  @OneToMany(() => PaymentStatusDb, (paymentStatus) => paymentStatus.payment)
  statuses!: PaymentStatusDb[];
}
