import { Shipment } from '../entity/shipment/shipment';

export interface IShipmentRepository {
  save(shipment: Shipment): Promise<Shipment>;
  findById(shipmentId: string): Promise<Shipment | null>;
  update(shipment: Shipment): Promise<Shipment>;
  findShipmentsByOrderId(orderId: string): Promise<Shipment[]>;
}
