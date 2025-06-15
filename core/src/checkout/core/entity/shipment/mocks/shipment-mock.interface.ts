import { Shipment, ShipmentData } from '../shipment';

export abstract class IShipmentMockService {
  abstract getOneToCreate(userIdOverrides?: string): string;
  abstract getOne(overrides: Partial<ShipmentData>): Shipment;
  abstract createOne(overrides: Partial<ShipmentData>): Promise<Shipment>;
}
