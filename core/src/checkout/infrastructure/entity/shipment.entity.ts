import { Entity, Column, PrimaryColumn, OneToOne, JoinColumn } from 'typeorm';
import { BaseEntity } from 'src/shared/types/base-entity/base.entity';
import { OrderDb } from './order.entity';
import { ShipmentStatus } from 'src/checkout/core/entity/shipment/shipment';

@Entity('shipments')
export class ShipmentDb extends BaseEntity {
  @PrimaryColumn()
  id!: string;

  @OneToOne(() => OrderDb, (order) => order.shipment, { onDelete: 'CASCADE' })
  @JoinColumn({ name: 'order_id' })
  order!: OrderDb;

  @Column({
    type: 'enum',
    enum: ShipmentStatus,
    default: ShipmentStatus.Pending,
  })
  shipmentStatus!: ShipmentStatus;

  @Column({ type: 'varchar', length: 255 })
  trackingNumber!: string;

  @Column({ type: 'timestamp', nullable: true })
  shippedAt?: Date;

  @Column({ type: 'timestamp', nullable: true })
  estimatedArrival?: Date;
}
