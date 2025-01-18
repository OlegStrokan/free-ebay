import { Inject, Injectable } from '@nestjs/common';
import { Repository } from 'typeorm';
import { InjectRepository } from '@nestjs/typeorm';
import { ShipmentDb } from '../entity/shipment.entity';
import { IShipmentRepository } from 'src/checkout/core/repository/shipment.repository';
import {
  Shipment,
  ShipmentData,
} from 'src/checkout/core/entity/shipment/shipment';
import { SHIPMENT_MAPPER } from 'src/checkout/epplication/injection-tokens/mapper.token';
import { IShipmentMapper } from '../mappers/shipment/shipment.mapper.interface';
import { IClearableRepository } from 'src/shared/types/clearable';

@Injectable()
export class ShipmentRepository
  implements IShipmentRepository, IClearableRepository
{
  constructor(
    @InjectRepository(ShipmentDb)
    private readonly shipmentRepository: Repository<ShipmentDb>,
    @Inject(SHIPMENT_MAPPER)
    private readonly mapper: IShipmentMapper<
      ShipmentData,
      Shipment,
      ShipmentDb
    >,
  ) {}

  async save(shipment: Shipment): Promise<Shipment> {
    const dbShipment = this.mapper.toDb(shipment.data);
    const createdShipment = await this.shipmentRepository.save(dbShipment);
    return this.mapper.toDomain(createdShipment);
  }

  async findById(shipmentId: string): Promise<Shipment | null> {
    const shipment = await this.shipmentRepository.findOneBy({
      id: shipmentId,
    });
    return shipment ? this.mapper.toDomain(shipment) : null;
  }

  async update(shipment: Shipment): Promise<Shipment> {
    const dbShipment = this.mapper.toDb(shipment.data);

    const updatedShipment = await this.shipmentRepository.save(dbShipment);

    return this.mapper.toDomain(updatedShipment);
  }

  async findShipmentsByOrderId(orderId: string): Promise<Shipment[]> {
    const shipments = await this.shipmentRepository.find({
      where: { order: { id: orderId } },
      relations: ['order'],
    });

    return shipments.map((shipment) => this.mapper.toDomain(shipment));
  }

  async clear(): Promise<void> {
    await this.shipmentRepository.query(`DELETE FROM "shipments"`);
  }
}
