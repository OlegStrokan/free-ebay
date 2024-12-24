import { Order } from 'src/order/domain/order';
import { OrderItem } from 'src/order-item/domain/entity/order-item';
import { Parcel } from './parcel';

export interface IParcelCommandRepository {
    insertMany(order: Order): Promise<void>;
    insertOne(parcel: Parcel, item: OrderItem): Promise<void>;
    updateOne(parcel: Parcel): Promise<void>;
    deleteOne(parcelId: string): Promise<void>;
}
