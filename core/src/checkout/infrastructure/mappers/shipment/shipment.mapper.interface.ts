import { Shipment } from 'src/checkout/core/entity/shipment/shipment';
import { ShipmentDb } from '../../entity/shipment.entity';
import { ShipmentData } from 'src/checkout/core/entity/shipment/shipment';

export abstract class IShipmentMapper {
  abstract toDb(domain: ShipmentData): ShipmentDb;
  abstract toDomain(db: ShipmentDb): Shipment;
  abstract toClient(domain: Shipment): ShipmentData;
}
