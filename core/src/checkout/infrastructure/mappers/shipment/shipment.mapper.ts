import {
  Shipment,
  ShipmentData,
} from 'src/checkout/core/entity/shipment/shipment';
import { ShipmentDb } from '../../entity/shipment.entity';
import { IShipmentMapper } from './shipment.mapper.interface';
import { generateUlid } from 'src/shared/types/generate-ulid';

export class ShipmentMapper implements IShipmentMapper {
  toDomain(shipmentDb: ShipmentDb): Shipment {
    const shipmentData: ShipmentData = {
      id: shipmentDb.id,
      // TODO fix it
      orderId: shipmentDb?.order?.id ?? generateUlid(),
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
    // Assuming order is set elsewhere
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
