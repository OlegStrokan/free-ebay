import { Shipment, ShipmentData } from '../shipment';

export interface IShipmentMockService {
  getOneToCreate(userIdOverrides?: string): string;
  getOne(overrides: Partial<ShipmentData>): Shipment;
  createOne(overrides: Partial<ShipmentData>): Promise<Shipment>;
}
