// parcel-query.mapper.ts
import { Parcel } from 'src/parcel/domain/parcel';
import { OrderItemQueryMapper } from '../order-item/order-item-query.mapper';
import { HasMany } from 'src/libs/helpers/db-relationship.interface';
import { ParcelQuery } from 'src/parcel/infrastructure/entity/parcel-query.entity';

export class ParcelQueryMapper {
    static toDomain(parcelQuery: ParcelQuery): Parcel {
        const items = parcelQuery.items
            ? HasMany.loaded(
                  parcelQuery.items.map((item) => OrderItemQueryMapper.toDomain(item)),
                  'parcel.items'
              )
            : HasMany.unloaded('parcel.items');

        return new Parcel({
            ...parcelQuery,
            items,
        });
    }

    static toEntity(parcel: Parcel): ParcelQuery {
        const parcelQuery = new ParcelQuery();
        parcelQuery.id = parcel.id;
        parcelQuery.trackingNumber = parcel.trackingNumber;
        parcelQuery.weight = parcel.weight;
        parcelQuery.dimensions = parcel.dimensions;
        parcelQuery.orderId = parcel.orderId;

        if (parcel.data.items.isLoaded()) {
            parcelQuery.items = parcel.data.items.get().map((item) => OrderItemQueryMapper.toEntity(item));
        }

        return parcelQuery;
    }
}
