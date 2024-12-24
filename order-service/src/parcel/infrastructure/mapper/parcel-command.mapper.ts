import { HasMany } from 'src/libs/helpers/db-relationship.interface';
import { OrderItemCommandMapper } from 'src/order-item/infrastructure/mapper/order-item-command.mapper';
import { Parcel } from 'src/parcel/domain/parcel';
import { ParcelDto } from 'src/parcel/interface/dto/parcel.dto';
import { ParcelCommand } from '../entity/parcel-command.entity';

export class ParcelCommandMapper {
    static toDomain(parcelCommand: ParcelCommand): Parcel {
        const items = parcelCommand.items
            ? HasMany.loaded(
                  parcelCommand.items.map((item) => OrderItemCommandMapper.toDomain(item)),
                  'parcel.items'
              )
            : HasMany.unloaded('parcel.items');

        return new Parcel({
            ...parcelCommand,
            items,
        });
    }

    static toClient(parcel: Parcel): ParcelDto {
        return {
            id: parcel.id,
            createdAt: parcel.createdAt,
            updatedAt: parcel.updatedAt,
            dimensions: parcel.dimensions,
            orderId: parcel.orderId,
            trackingNumber: parcel.trackingNumber,
            weight: parcel.weight,
        };
    }

    static toEntity(parcel: Parcel): ParcelCommand {
        const parcelCommand = new ParcelCommand();
        parcelCommand.id = parcel.id;
        parcelCommand.trackingNumber = parcel.trackingNumber;
        parcelCommand.weight = parcel.weight;
        parcelCommand.dimensions = parcel.dimensions;
        parcelCommand.orderId = parcel.orderId;

        if (parcel.data.items.isLoaded()) {
            parcelCommand.items = parcel.data.items.get().map((item) => OrderItemCommandMapper.toEntity(item));
        }

        return parcelCommand;
    }
}
