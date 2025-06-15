import { Shipment } from '../entity/shipment/shipment';

export abstract class IShipmentRepository {
  abstract save(shipment: Shipment): Promise<Shipment>;
  abstract findById(shipmentId: string): Promise<Shipment | null>;
  abstract update(shipment: Shipment): Promise<Shipment>;
  abstract findShipmentsByOrderId(orderId: string): Promise<Shipment[]>;
}
