import { BaseEntity } from 'src/libs/database/base.entity';
import { OrderItemQuery } from 'src/order-item/infrastructure/entity/order-item-query.entity';
import { ParcelQuery } from 'src/parcel/infrastructure/entity/parcel-query.entity';
import { RepaymentPreferencesQuery } from 'src/repayment-preferences/infrastructure/entity/repayment-preferences-query.entity';
import { Column, Entity, OneToMany, PrimaryColumn } from 'typeorm';

@Entity('order_query')
export class OrderQuery extends BaseEntity {
    @PrimaryColumn()
    id: string;

    @Column()
    customerId: string;

    @Column({ type: 'decimal', precision: 10, scale: 2 })
    totalAmount: number;

    @Column()
    status: string;

    @Column({ nullable: true })
    deliveryAddress: string;

    @Column({ nullable: true })
    paymentMethod: string;

    @Column({ nullable: true })
    deliveryDate: Date;

    @Column({ nullable: true })
    trackingNumber: string;

    @Column({ nullable: true })
    feedback: string;

    @Column({ nullable: true })
    specialInstructions: string;

    @OneToMany(() => OrderItemQuery, (item) => item.orderQuery, {
        cascade: true,
    })
    items: OrderItemQuery[];

    @OneToMany(() => ParcelQuery, (parcel) => parcel.orderId)
    parcels: ParcelQuery[];

    @OneToMany(() => RepaymentPreferencesQuery, (repaymentPreferences) => repaymentPreferences.order)
    repaymentPreferences: RepaymentPreferencesQuery[];
}
