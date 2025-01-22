import { Inject, Injectable } from '@nestjs/common';
import { IShipmentRepository } from 'src/checkout/core/repository/shipment.repository';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { faker } from '@faker-js/faker';
import { SHIPMENT_REPOSITORY } from 'src/checkout/epplication/injection-tokens/repository.token';
import { ShipmentData, ShipmentStatus, Shipment } from '../shipment';
import { IShipmentMockService } from './shipment-mock.interface';

@Injectable()
export class ShipmentMockService implements IShipmentMockService {
  constructor(
    @Inject(SHIPMENT_REPOSITORY)
    private readonly shipmentRepository: IShipmentRepository,
  ) {}

  getOneToCreate(overridesUserId: string): string {
    return overridesUserId ?? generateUlid();
  }

  getOne(overrides: Partial<ShipmentData> = {}): Shipment {
    const shipmentData: ShipmentData = {
      id: overrides.id ?? generateUlid(),
      orderId: overrides.orderId ?? generateUlid(),
      shipmentStatus: overrides.shipmentStatus ?? ShipmentStatus.Pending,
      trackingNumber: overrides.trackingNumber ?? faker.string.uuid(),
      shippedAt: overrides.shippedAt ?? new Date(),
      shippingAddress:
        overrides.shipmentStatus ?? faker.location.streetAddress(),
      estimatedArrival: overrides.estimatedArrival ?? new Date(),
      createdAt: overrides.createdAt ?? new Date(),
      updatedAt: overrides.updatedAt ?? new Date(),
    };

    return new Shipment(shipmentData);
  }

  async createOne(overrides: Partial<ShipmentData> = {}): Promise<Shipment> {
    const shipment = this.getOne(overrides);
    return await this.shipmentRepository.save(shipment);
  }
}
