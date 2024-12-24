import { HasMany } from 'src/libs/helpers/db-relationship.interface';
import { OrderItem } from 'src/order-item/domain/entity/order-item';
import { OrderItemQuery } from 'src/order-item/infrastructure/entity/order-item-query.entity';
import { Order, OrderData } from 'src/order/domain/order';
import { OrderQuery } from 'src/order/infrastructure/entity/order-query.entity';
import { Parcel } from 'src/parcel/domain/parcel';
import { ParcelQuery } from 'src/parcel/infrastructure/entity/parcel-query.entity';
import { OrderStatusMapper } from '../order-status.mapper';

export class OrderQueryMapper {
    static toDomain(orderQuery: OrderQuery): Order {
        const orderData: OrderData = {
            id: orderQuery.id,
            customerId: orderQuery.customerId,
            totalAmount: orderQuery.totalAmount,
            createdAt: orderQuery.createdAt,
            updatedAt: orderQuery.updatedAt,
            status: OrderStatusMapper.fromDatabase(orderQuery.status),
            deliveryAddress: orderQuery.deliveryAddress,
            paymentMethod: orderQuery.paymentMethod,
            deliveryDate: orderQuery.deliveryDate,
            trackingNumber: orderQuery.trackingNumber,
            specialInstructions: orderQuery.specialInstructions,
            feedback: orderQuery.feedback,
            items: orderQuery.items?.map((item) => OrderQueryMapper.toDomainItem(item)),
            parcels: orderQuery.parcels?.map((parcel) => OrderQueryMapper.toDomainParcel(parcel)),
        };
        return Order.createWithId(orderData);
    }

    static toClient(orderQuery: OrderQuery): any {
        return {
            id: orderQuery.id,
            customerId: orderQuery.customerId,
            totalAmount: orderQuery.totalAmount,
            createdAt: orderQuery.createdAt,
            updatedAt: orderQuery.updatedAt,
            status: OrderStatusMapper.fromDatabase(orderQuery.status),
            deliveryAddress: orderQuery.deliveryAddress,
            paymentMethod: orderQuery.paymentMethod,
            deliveryDate: orderQuery.deliveryDate,
            trackingNumber: orderQuery.trackingNumber,
            specialInstructions: orderQuery.specialInstructions,
            feedback: orderQuery.feedback,
        };
    }

    static toDomainItem(itemQuery: OrderItemQuery): OrderItem {
        return OrderItem.createWithIdAndDate({
            id: itemQuery.id,
            productId: itemQuery.productId,
            quantity: itemQuery.quantity,
            price: itemQuery.price,
            weight: itemQuery.weight,
            createdAt: itemQuery.createdAt,
            updatedAt: itemQuery.updatedAt,
        });
    }

    static toDomainParcel(parcel: ParcelQuery): Parcel {
        const domainItems = HasMany.loaded(
            parcel.items?.map((item) => OrderQueryMapper.toDomainItem(item)) || [],
            'parcel.items'
        );

        return Parcel.create({
            ...parcel,
            items: domainItems,
        });
    }

    static toEntity(order: Order): OrderQuery {
        const orderQuery = new OrderQuery();
        orderQuery.id = order.id;
        orderQuery.customerId = order.customerId;
        orderQuery.totalAmount = order.totalAmount;
        orderQuery.createdAt = order.createdAt;
        orderQuery.updatedAt = order.updatedAt;
        orderQuery.status = order.status;
        orderQuery.deliveryAddress = order.deliveryAddress;
        orderQuery.paymentMethod = order.paymentMethod;
        orderQuery.deliveryDate = order.deliveryDate;
        orderQuery.trackingNumber = order.trackingNumber;
        orderQuery.feedback = order.feedback;
        orderQuery.items = order.items.map((item) => OrderQueryMapper.toEntityItem(item));
        return orderQuery;
    }

    static toEntityItem(item: OrderItem): OrderItemQuery {
        const itemQuery = new OrderItemQuery();
        itemQuery.productId = item.productId;
        itemQuery.quantity = item.quantity;
        itemQuery.price = item.price;
        return itemQuery;
    }

    static toEntityParcel(parcel: Parcel): ParcelQuery {
        const dbParcel = new ParcelQuery();
        dbParcel.dimensions = parcel.dimensions;
        dbParcel.id = parcel.id;
        dbParcel.items = parcel.data.items.isLoaded()
            ? parcel.data.items.get().map((item) => OrderQueryMapper.toEntityItem(item))
            : [];
        dbParcel.orderId = parcel.orderId;
        dbParcel.trackingNumber = parcel.trackingNumber;
        dbParcel.weight = parcel.weight;
        dbParcel.createdAt = parcel.data.createdAt;
        dbParcel.updatedAt = parcel.data.updatedAt;

        return dbParcel;
    }
}
