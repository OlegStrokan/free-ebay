import { Entity, Column, PrimaryColumn, JoinColumn, ManyToOne } from 'typeorm';
import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { CartDb } from './cart.entity';

@Entity('cart_items')
export class CartItemDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @ManyToOne(() => CartDb, (cart) => cart.items, { onDelete: 'CASCADE' })
  @JoinColumn({ name: 'cartId' })
  cart!: CartDb;

  @Column({ type: 'varchar' })
  cartId!: string;

  @Column({ type: 'varchar' })
  productId!: string;

  @Column()
  quantity!: number;

  @Column({ type: 'jsonb' })
  price!: string;
}
