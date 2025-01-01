import { UserDb } from 'src/user/infrastructure/entity/user.entity';
import {
  Entity,
  ManyToOne,
  OneToMany,
  JoinColumn,
  PrimaryColumn,
  BaseEntity,
  OneToOne,
} from 'typeorm';
import { OrderItemDb } from './order-item.entity';
import { ShipmentDb } from './shipment.entity';
import { PaymentDb } from './payment.entity';

@Entity('orders')
export class OrderDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @ManyToOne(() => UserDb, (user) => user.orders, { onDelete: 'CASCADE' })
  @JoinColumn({ name: 'user_id' })
  user!: UserDb;

  @OneToMany(() => OrderItemDb, (orderItem) => orderItem.order)
  orderItems!: OrderItemDb[];

  @OneToOne(() => ShipmentDb, (shipment) => shipment.order, {
    onDelete: 'CASCADE',
  })
  @JoinColumn({ name: 'shipment_id' })
  shipment!: ShipmentDb;

  @OneToOne(() => PaymentDb, (payment) => payment.order, {
    onDelete: 'CASCADE',
  })
  payment!: PaymentDb;
}
