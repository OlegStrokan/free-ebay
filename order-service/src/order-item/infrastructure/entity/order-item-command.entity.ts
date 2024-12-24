import { Column, Entity, JoinColumn, ManyToOne, PrimaryColumn } from 'typeorm';
import { BaseEntity } from 'src/libs/database/base.entity';
import { OrderCommand } from 'src/order/infrastructure/entity/order-command.entity';
import { ParcelCommand } from 'src/parcel/infrastructure/entity/parcel-command.entity';

@Entity('order_item_command')
export class OrderItemCommand extends BaseEntity {
    @PrimaryColumn()
    id: string;

    @Column()
    productId: string;

    @Column()
    quantity: number;

    @Column({ type: 'decimal', precision: 10, scale: 2 })
    price: number;

    @Column({ type: 'decimal', precision: 10, scale: 2 })
    weight: number;

    @ManyToOne(() => OrderCommand, (order) => order.items)
    @JoinColumn({ name: 'orderCommandId' })
    orderCommand: OrderCommand;

    @ManyToOne(() => ParcelCommand, (parcel) => parcel.items, { nullable: true })
    parcel: ParcelCommand;
}
