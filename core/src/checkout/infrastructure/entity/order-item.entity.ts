import { ProductDb } from 'src/product/infrastructure/entity/product.entity';
import {
  Entity,
  Column,
  ManyToOne,
  JoinColumn,
  BaseEntity,
  PrimaryColumn,
} from 'typeorm';
import { OrderDb } from './order.entity';

@Entity('order_items')
export class OrderItemDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @ManyToOne(() => OrderDb, (order) => order.orderItems, {
    onDelete: 'CASCADE',
  })
  @JoinColumn({ name: 'order_id' })
  order!: OrderDb;

  @ManyToOne(() => ProductDb, { onDelete: 'SET NULL' })
  @JoinColumn({ name: 'product_id' })
  product!: ProductDb;

  @Column({ type: 'int' })
  quantity!: number;

  @Column({ type: 'decimal' })
  price!: number;
}
