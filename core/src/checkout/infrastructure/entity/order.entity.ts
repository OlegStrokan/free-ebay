import { UserDb } from 'src/user/infrastructure/entity/user.entity';
import {
  Entity,
  ManyToOne,
  OneToMany,
  JoinColumn,
  PrimaryColumn,
  OneToOne,
  Column,
} from 'typeorm';
import { OrderItemDb } from './order-item.entity';
import { ShipmentDb } from './shipment.entity';
import { PaymentDb } from './payment.entity';
import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { OrderStatus } from 'src/checkout/core/entity/order/order';

@Entity('orders')
export class OrderDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @Column({ type: 'jsonb' })
  totalPrice!: string;

  @Column({
    type: 'enum',
    enum: OrderStatus,
    default: OrderStatus.Shipped,
  })
  status!: OrderStatus;

  @ManyToOne(() => UserDb, (user) => user.orders, { onDelete: 'CASCADE' })
  @JoinColumn({ name: 'user_id' })
  user!: UserDb;

  @OneToMany(() => OrderItemDb, (orderItem) => orderItem.order, {
    cascade: true,
    orphanedRowAction: 'delete',
  })
  items!: OrderItemDb[];

  @OneToOne(() => ShipmentDb, (shipment) => shipment.order, {
    onDelete: 'CASCADE',
    cascade: true,
  })
  @JoinColumn({ name: 'shipment_id' })
  shipment!: ShipmentDb;

  @OneToOne(() => PaymentDb, (payment) => payment.order, {
    onDelete: 'CASCADE',
  })
  payment!: PaymentDb;
}
