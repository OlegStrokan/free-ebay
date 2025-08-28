import {
  Shipment,
  ShipmentData,
} from 'src/checkout/core/entity/shipment/shipment';
import { ShipmentDb } from '../../entity/shipment.entity';
import { IShipmentMapper } from './shipment.mapper.interface';
import { OrderDb } from '../../entity/order.entity';
import { Injectable } from '@nestjs/common';

@Injectable()
export class ShipmentMapper implements IShipmentMapper {
  toDomain(shipmentDb: ShipmentDb): Shipment {
    if (!shipmentDb.order?.id) {
      throw new Error(
        `Shipment with id ${shipmentDb.id} is missing its associated order. Ensure the order relation is loaded.`,
      );
    }

    const shipmentData: ShipmentData = {
      id: shipmentDb.id,
      orderId: shipmentDb.order.id,
      shipmentStatus: shipmentDb.shipmentStatus,
      shippingAddress: shipmentDb.address,
      trackingNumber: shipmentDb.trackingNumber,
      shippedAt: shipmentDb.shippedAt,
      estimatedArrival: shipmentDb.estimatedArrival,
      createdAt: shipmentDb.createdAt,
      updatedAt: shipmentDb.updatedAt,
    };

    return new Shipment(shipmentData);
  }

  toDb(shipment: ShipmentData): ShipmentDb {
    const shipmentDb = new ShipmentDb();
    shipmentDb.id = shipment.id;
    shipmentDb.order = { id: shipment.orderId } as OrderDb;
    shipmentDb.shipmentStatus = shipment.shipmentStatus;
    shipmentDb.trackingNumber = shipment.trackingNumber;
    shipmentDb.address = shipment.shippingAddress;
    shipmentDb.shippedAt = shipment.shippedAt;
    shipmentDb.estimatedArrival = shipment.estimatedArrival;
    shipmentDb.createdAt = shipment.createdAt;
    shipmentDb.updatedAt = shipment.updatedAt;
    return shipmentDb;
  }

  toClient(shipment: Shipment): ShipmentData {
    return shipment.data;
  }
}
