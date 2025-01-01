import {
  Entity,
  Column,
  PrimaryColumn,
  JoinColumn,
  ManyToOne,
  OneToMany,
} from 'typeorm';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { CategoryDb } from 'src/catalog/infrastructure/entity/category';
import { OrderItemDb } from 'src/checkout/infrastructure/entity/order-item.entity';

@Entity('products')
export class ProductDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @Column({ type: 'varchar', length: 100, unique: true })
  sku!: string;

  @Column({
    type: 'enum',
    enum: ProductStatus,
    default: ProductStatus.Available,
  })
  status!: ProductStatus;

  @Column({ type: 'jsonb' })
  price!: string;

  @Column({ type: 'varchar', length: 255 })
  name!: string;

  @Column({ type: 'text' })
  description!: string;

  @Column({ type: 'timestamp', nullable: true })
  discontinuedAt?: Date;

  @Column({ type: 'int', default: 0 })
  stock?: number;

  @ManyToOne(() => CategoryDb, (category) => category.products, {
    onDelete: 'SET NULL',
  })
  @JoinColumn({ name: 'category_id' })
  category!: CategoryDb;

  @OneToMany(() => OrderItemDb, (orderItem) => orderItem.product)
  orderItems!: OrderItemDb[];
}
