import { Order } from 'src/order/domain/order';
import { Parcel } from './parcel';
import { OrderItem } from 'src/order-item/domain/entity/order-item';

export interface IParcelQueryRepository {
    find(): Promise<Parcel[]>;
    findById(parcelId: string): Promise<Parcel>;
    insertMany(order: Order): Promise<void>;
    insertOne(parcel: Parcel, item: OrderItem): Promise<void>;
    updateOne(parcelId: string, parcel: Partial<Parcel>): Promise<void>;
    deleteOne(parcelId: string): Promise<void>;
}
