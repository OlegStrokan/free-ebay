import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { Entity, Column, ManyToOne, JoinColumn, PrimaryColumn } from 'typeorm';
import { PaymentDb } from './payment.entity';

@Entity('payment_status')
export class PaymentStatusDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @Column({ type: 'varchar', length: 100 })
  status!: string;

  @ManyToOne(() => PaymentDb, (payment) => payment.statuses)
  @JoinColumn({ name: 'paymentId' })
  payment!: PaymentDb;
}
