import { Entity, Column, ManyToOne, JoinColumn, PrimaryColumn } from 'typeorm';
import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { OrderDb } from './order.entity';

@Entity('order_items')
export class OrderItemDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @Column({ type: 'varchar' })
  productId!: string;

  @Column()
  quantity!: number;

  @Column({ type: 'jsonb' })
  priceAtPurchase!: string;

  @ManyToOne(() => OrderDb, (order) => order.items, { onDelete: 'CASCADE' })
  @JoinColumn({ name: 'orderId' })
  order!: OrderDb;

  @Column({ type: 'varchar' })
  orderId!: string;
}
