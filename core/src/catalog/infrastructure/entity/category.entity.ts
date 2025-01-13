import { ProductDb } from 'src/product/infrastructure/entity/product.entity';
import {
  Entity,
  Column,
  OneToMany,
  ManyToOne,
  JoinColumn,
  PrimaryColumn,
} from 'typeorm';

@Entity('categories')
export class CategoryDb {
  @PrimaryColumn()
  id!: string;

  @Column({ unique: true })
  name!: string;

  @Column({ nullable: true })
  description!: string;

  @ManyToOne(() => CategoryDb, (category) => category.children, {
    nullable: true,
  })
  @JoinColumn({ name: 'parentCategoryId' })
  parentCategory?: CategoryDb;

  @OneToMany(() => CategoryDb, (category) => category.parentCategory)
  children!: CategoryDb[];

  @OneToMany(() => ProductDb, (product) => product.category)
  products!: ProductDb[];
}
