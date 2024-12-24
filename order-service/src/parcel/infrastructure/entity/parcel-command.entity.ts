import { Entity, Column, ManyToOne, PrimaryColumn, OneToMany } from 'typeorm';
import { BaseEntity } from 'src/libs/database/base.entity';
import { OrderItemCommand } from 'src/order-item/infrastructure/entity/order-item-command.entity';
import { OrderCommand } from 'src/order/infrastructure/entity/order-command.entity';
import { ShippingCostCommand } from 'src/shipping-cost/infrastructure/entity/shipping-cost-command.entity';
import { Dimension } from 'src/shipping-cost/domain/shipping-cost';

@Entity('parcel_command')
export class ParcelCommand extends BaseEntity {
    @PrimaryColumn()
    id: string;

    @Column()
    trackingNumber: string;

    @Column('decimal')
    weight: number;

    @Column()
    dimensions: Dimension;

    @Column()
    orderId: string;

    @ManyToOne(() => OrderCommand, (order) => order.parcels)
    order: OrderCommand;

    @OneToMany(() => OrderItemCommand, (item) => item.parcel)
    items: OrderItemCommand[];

    @ManyToOne(() => ShippingCostCommand, (shippingCost) => shippingCost.parcels, { nullable: true })
    shippingCost: ShippingCostCommand;
}
