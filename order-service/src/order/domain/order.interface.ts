import { OrderItem } from 'src/order-item/domain/entity/order-item';

export interface IOrder {
    compareId: (id: string) => boolean;
    create: () => void;
    cancel: () => void;
    ship: (trackingNumber: string, deliveryDate: Date) => void;
    addItem: (item: OrderItem) => void;
    commit: () => void;
}
