import {
  Entity,
  Column,
  PrimaryColumn,
  OneToOne,
  ManyToOne,
  JoinColumn,
} from 'typeorm';
import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { OrderDb } from './order.entity';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';
import {
  PaymentMethod,
  PaymentStatus,
} from 'src/checkout/core/entity/payment/payment';

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

  @Column({ type: 'enum', enum: PaymentStatus, default: PaymentStatus.Pending })
  paymentStatus!: PaymentStatus;

  @Column({ type: 'enum', enum: PaymentMethod })
  paymentMethod!: PaymentMethod;

  @Column({ type: 'jsonb' })
  amount!: string;

  @Column({ type: 'timestamp', default: () => 'CURRENT_TIMESTAMP' })
  paymentDate!: Date;

  @Column({ type: 'varchar', length: 100, nullable: true })
  paymentIntentId?: string;
}
