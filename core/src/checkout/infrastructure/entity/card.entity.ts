import { ProductDb } from 'src/product/infrastructure/entity/product.entity';
import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';
import {
  Entity,
  OneToOne,
  ManyToMany,
  JoinTable,
  PrimaryColumn,
} from 'typeorm';

@Entity('carts')
export class CartDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @OneToOne(() => UserDb, (user) => user.cart, { onDelete: 'CASCADE' })
  user!: UserDb;

  @ManyToMany(() => ProductDb)
  @JoinTable()
  products!: ProductDb[];
}
