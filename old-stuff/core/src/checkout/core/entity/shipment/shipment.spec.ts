import { Shipment, ShipmentData, ShipmentStatus } from './shipment';
import { generateUlid } from 'src/shared/types/generate-ulid';

describe('Shipment', () => {
  let shipmentData: ShipmentData;
  let shipment: Shipment;

  beforeEach(() => {
    shipmentData = {
      id: generateUlid(),
      orderId: 'order1',
      shipmentStatus: ShipmentStatus.Pending,
      trackingNumber: 'shipment-order1-2023-01-01T00:00:00.000Z',
      createdAt: new Date(),
      updatedAt: new Date(),
      shippingAddress: 'address1',
    };
    shipment = new Shipment(shipmentData);
  });

  test('should create a shipment successfully', () => {
    const newShipment = Shipment.create('order2', 'address1');
    expect(newShipment).toBeInstanceOf(Shipment);
    expect(newShipment.orderId).toBe('order2');
    expect(newShipment.shipmentStatus).toBe(ShipmentStatus.Pending);
    expect(newShipment.trackingNumber).toMatch(
      /shipment-order2-\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z/,
    );
  });

  test('should update shipment status successfully', () => {
    const updatedShipment = shipment.updateStatus(
      ShipmentStatus.Shipped,
      new Date(),
      new Date(),
    );
    expect(updatedShipment.shipmentStatus).toBe(ShipmentStatus.Shipped);
    expect(updatedShipment.shippedAt).toBeInstanceOf(Date);
  });

  test('should retain original shipment data after status update', () => {
    const updatedShipment = shipment.updateStatus(ShipmentStatus.Shipped);
    expect(shipment.shipmentStatus).toBe(ShipmentStatus.Pending);
    expect(updatedShipment.shipmentStatus).toBe(ShipmentStatus.Shipped);
  });

  test('should update shipment status with estimated arrival', () => {
    const estimatedArrival = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000);
    const updatedShipment = shipment.updateStatus(
      ShipmentStatus.Delivered,
      new Date(),
      estimatedArrival,
    );
    expect(updatedShipment.estimatedArrival).toEqual(estimatedArrival);
  });
});
