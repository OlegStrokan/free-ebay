import {
  Entity,
  PrimaryGeneratedColumn,
  Column,
  BaseEntity,
  CreateDateColumn,
  UpdateDateColumn,
} from 'typeorm';
import { Money } from 'src/shared/types/Money';
import { ProductStatus } from 'src/product/core/product/entity/product-status';

@Entity('products')
export class ProductDb extends BaseEntity {
  @PrimaryGeneratedColumn('uuid')
  id: string;

  @Column({ type: 'varchar', length: 100, unique: true })
  sku: string;

  @Column({
    type: 'enum',
    enum: ProductStatus,
    default: ProductStatus.Available,
  })
  status: ProductStatus;

  @Column({ type: 'decimal', precision: 10, scale: 2, nullable: true })
  price: Money;

  @Column({ type: 'varchar', length: 255, nullable: true })
  category: string;

  @Column({ type: 'timestamp', nullable: true })
  discontinuedAt: string | null;

  @CreateDateColumn()
  createdAt: string;

  @UpdateDateColumn()
  updatedAt: string;
}
