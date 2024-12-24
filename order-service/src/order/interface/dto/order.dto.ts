import { OrderItemDto } from 'src/order-item/interface/dto/order-item.dto';
import { OrderStatus } from 'src/order/domain/order';
import { ParcelDto } from 'src/parcel/interface/dto/parcel.dto';

export class OrderDto {
    id: string;
    customerId: string;
    totalAmount: number;
    items: OrderItemDto[];
    status: OrderStatus;
    parcels?: ParcelDto[];
    updatedAt?: Date;
    version?: number;
    trackingNumber?: string;
    deliveryDate?: Date;
    feedback?: string;
    deliveryAddress?: string;
    paymentMethod?: string;
    specialInstructions?: string;
}
