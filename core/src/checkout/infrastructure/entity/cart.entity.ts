import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { Entity, PrimaryColumn, Column, OneToMany } from 'typeorm';
import { CartItemDb } from './cart-item.entity';

@Entity('carts')
export class CartDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @Column({ type: 'varchar' })
  userId!: string;

  @Column({ type: 'jsonb' })
  totalPrice!: string;

  @OneToMany(() => CartItemDb, (cartItem) => cartItem.cart, {
    cascade: true,
    orphanedRowAction: 'delete',
  })
  items!: CartItemDb[];
}
