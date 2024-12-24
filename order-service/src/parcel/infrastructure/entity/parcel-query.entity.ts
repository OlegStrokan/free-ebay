import { Entity, Column, PrimaryColumn, OneToMany, ManyToOne } from 'typeorm';
import { BaseEntity } from 'src/libs/database/base.entity';
import { OrderItemQuery } from 'src/order-item/infrastructure/entity/order-item-query.entity';
import { Dimension } from 'src/shipping-cost/domain/shipping-cost';
import { ShippingCostQuery } from 'src/shipping-cost/infrastructure/entity/shipping-cost-query.entity';

@Entity('parcel_query')
export class ParcelQuery extends BaseEntity {
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

    @OneToMany(() => OrderItemQuery, (item) => item.parcel)
    items: OrderItemQuery[];

    @ManyToOne(() => ShippingCostQuery, (shippingCost) => shippingCost.parcels, { nullable: true })
    shippingCost: ShippingCostQuery;
}
