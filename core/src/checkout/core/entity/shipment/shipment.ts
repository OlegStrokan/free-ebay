import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';

export enum ShipmentStatus {
  Pending = 'Pending',
  Shipped = 'Shipped',
  InTransit = 'InTransit',
  Delivered = 'Delivered',
  Returned = 'Returned',
  Cancelled = 'Cancelled',
}

export interface ShipmentData {
  id: string;
  orderId: string;
  shipmentStatus: ShipmentStatus;
  trackingNumber: string;
  shippedAt?: Date;
  estimatedArrival?: Date;
  createdAt: Date;
  updatedAt: Date;
}

export class Shipment implements Clonable<Shipment> {
  constructor(public shipment: ShipmentData) {}

  static create = (orderId: string): Shipment => {
    const createdAt = new Date();
    const formattedDate = createdAt.toISOString();
    const generateTrackingNumber = `shipment-${orderId}-${formattedDate}`;
    return new Shipment({
      id: generateUlid(),
      orderId,
      shipmentStatus: ShipmentStatus.Pending,
      trackingNumber: generateTrackingNumber,
      createdAt,
      updatedAt: createdAt,
    });
  };

  get id(): string {
    return this.shipment.id;
  }

  get data(): ShipmentData {
    return this.shipment;
  }

  get orderId(): string {
    return this.shipment.orderId;
  }

  get shipmentStatus(): ShipmentStatus {
    return this.shipment.shipmentStatus;
  }

  get trackingNumber(): string {
    return this.shipment.trackingNumber;
  }

  get shippedAt(): Date | undefined {
    return this.shipment.shippedAt;
  }

  get estimatedArrival(): Date | undefined {
    return this.shipment.estimatedArrival;
  }

  updateStatus = (
    status: ShipmentStatus,
    shippedAt?: Date,
    estimatedArrival?: Date,
  ) => {
    const clone = this.clone();
    clone.shipment.shipmentStatus = status;
    if (shippedAt) clone.shipment.shippedAt = shippedAt;
    if (estimatedArrival) clone.shipment.estimatedArrival = estimatedArrival;
    return clone;
  };

  clone = (): Shipment => new Shipment({ ...this.shipment });
}
