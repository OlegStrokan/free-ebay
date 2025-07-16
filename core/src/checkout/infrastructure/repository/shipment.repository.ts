import { Injectable } from '@nestjs/common';
import { Repository } from 'typeorm';
import { InjectRepository } from '@nestjs/typeorm';
import { IShipmentRepository } from 'src/checkout/core/repository/shipment.repository';
import { Shipment } from 'src/checkout/core/entity/shipment/shipment';
import { IClearableRepository } from 'src/shared/types/clearable';
import { ShipmentDb } from '../entity/shipment.entity';
import { IShipmentMapper } from '../mappers/shipment/shipment.mapper.interface';
import { OrderDb } from '../entity/order.entity';

@Injectable()
export class ShipmentRepository
  implements IShipmentRepository, IClearableRepository
{
  constructor(
    @InjectRepository(ShipmentDb)
    private readonly shipmentRepository: Repository<ShipmentDb>,
    @InjectRepository(OrderDb)
    private readonly orderRepository: Repository<OrderDb>,
    private readonly mapper: IShipmentMapper,
  ) {}

  async save(shipment: Shipment): Promise<Shipment> {
    const dbShipment = this.mapper.toDb(shipment.data);
    const order = await this.orderRepository.findOneBy({
      id: shipment.data.orderId,
    });
    if (!order)
      throw new Error(`Order with id ${shipment.data.orderId} not found`);
    dbShipment.order = order;
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
