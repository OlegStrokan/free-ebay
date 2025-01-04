import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IShipmentMapper } from './shipment.mapper.interface';
import {
  Shipment,
  ShipmentData,
  ShipmentStatus,
} from 'src/checkout/core/entity/shipment/shipment';
import { ShipmentDb } from '../../entity/shipment.entity';
import { SHIPMENT_MAPPER } from 'src/checkout/epplication/injection-tokens/mapper.token';

const validateShipmentDataStructure = (
  shipmentData: ShipmentData | undefined,
) => {
  if (!shipmentData) throw new Error('Shipment not found test error');

  expect(shipmentData).toEqual({
    id: expect.any(String),
    orderId: expect.any(String),
    shipmentStatus: expect.any(String),
    trackingNumber: expect.any(String),
    shippedAt: expect.any(Date),
    estimatedArrival: expect.any(Date),
    createdAt: expect.any(Date),
    updatedAt: expect.any(Date),
  });
};

describe('ShipmentMapperTest', () => {
  let module: TestingModule;
  let shipmentMapper: IShipmentMapper<ShipmentData, Shipment, ShipmentDb>;

  beforeAll(async () => {
    module = await createTestingModule();

    shipmentMapper =
      module.get<IShipmentMapper<ShipmentData, Shipment, ShipmentDb>>(
        SHIPMENT_MAPPER,
      );
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully transform domain shipment to client (dto) shipment', async () => {
    const domainShipment = new Shipment({
      id: 'shipment123',
      orderId: 'order123',
      shipmentStatus: ShipmentStatus.Shipped,
      trackingNumber: 'tracking123',
      shippedAt: new Date(),
      estimatedArrival: new Date(),
      createdAt: new Date(),
      updatedAt: new Date(),
    });

    const dtoShipment = shipmentMapper.toClient(domainShipment);
    validateShipmentDataStructure(dtoShipment);
  });

  it('should successfully map database shipment to domain shipment', async () => {
    const shipmentDb = new ShipmentDb();
    shipmentDb.id = 'shipment123';
    shipmentDb.order = { id: 'order123' } as any; // Assuming order is loaded
    shipmentDb.shipmentStatus = ShipmentStatus.Shipped;
    shipmentDb.trackingNumber = 'tracking123';
    shipmentDb.shippedAt = new Date();
    shipmentDb.estimatedArrival = new Date();
    shipmentDb.createdAt = new Date();
    shipmentDb.updatedAt = new Date();

    const domainShipment = shipmentMapper.toDomain(shipmentDb);
    validateShipmentDataStructure(domainShipment.data);
  });
});
