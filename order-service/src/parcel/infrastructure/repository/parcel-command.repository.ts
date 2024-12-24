// parcel-command.repository.ts
import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { ParcelCommand } from '../entity/parcel-command.entity';
import { Repository } from 'typeorm';
import { OrderItem } from 'src/order-item/domain/entity/order-item';
import { Order } from 'src/order/domain/order';
import { Parcel } from 'src/parcel/domain/parcel';
import { IParcelCommandRepository } from 'src/parcel/domain/parcel-command.repository';

@Injectable()
export class ParcelCommandRepository implements IParcelCommandRepository {
    constructor(
        @InjectRepository(ParcelCommand, 'commandConnection')
        private readonly parcelRepository: Repository<ParcelCommand>
    ) {}
    public async insertMany(order: Order): Promise<void> {
        const parcels = Parcel.createParcels(order.id, order.items);
        await this.parcelRepository.save(parcels);
    }
    public async insertOne(parcel: Parcel, item: OrderItem): Promise<void> {
        const newParcel = Parcel.create({ ...parcel, items: [item] });
        await this.parcelRepository.save(newParcel);
    }
    public async updateOne(parcel: Parcel): Promise<void> {
        await this.parcelRepository.update(parcel.id, parcel);
    }
    public async deleteOne(parcelId: string): Promise<void> {
        await this.parcelRepository.delete(parcelId);
    }
}
