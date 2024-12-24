import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { ParcelCreateCommand } from 'src/parcel/application/command/parcel-create.command';
import { Parcel } from 'src/parcel/domain/parcel';
import { IParcelQueryRepository } from 'src/parcel/domain/parcel-query.repository';
import { Repository } from 'typeorm';
import { ParcelQuery } from '../entity/parcel-query.entity';
import { ParcelQueryMapper } from '../mapper/parcel-query.mapper';

@Injectable()
export class ParcelQueryRepository implements IParcelQueryRepository {
    constructor(
        @InjectRepository(ParcelQuery, 'queryConnection')
        private readonly parcelQueryRepository: Repository<ParcelQuery>
    ) {}

    public async findById(parcelId: string): Promise<Parcel | null> {
        const parcelEntity = await this.parcelQueryRepository.findOne({ where: { id: parcelId } });
        return ParcelQueryMapper.toDomain(parcelEntity);
    }

    public async insertMany(parcels: ParcelCreateCommand[]): Promise<void> {
        const createdParcels = parcels.map((parcel) => Parcel.create(parcel).data);
        await this.parcelQueryRepository.save(createdParcels);
    }
    public async insertOne(parcel: ParcelCreateCommand): Promise<void> {
        const newParcel = Parcel.create(parcel);
        await this.parcelQueryRepository.save(newParcel);
    }

    public async updateOne(orderId: string, updateData: Partial<Parcel>): Promise<void> {
        await this.parcelQueryRepository.update(orderId, updateData);
    }

    public async deleteOne(parcelId: string): Promise<void> {
        await this.parcelQueryRepository.delete(parcelId);
    }

    public async find(): Promise<Parcel[]> {
        const parcels = await this.parcelQueryRepository.find();
        return parcels.map((parcel) => ParcelQueryMapper.toDomain(parcel));
    }

    public async findOne(parcelId: string): Promise<Parcel> {
        const parcel = await this.parcelQueryRepository.findOneBy({ id: parcelId });
        return ParcelQueryMapper.toDomain(parcel);
    }
}
