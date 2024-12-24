import { OrderItem } from 'src/order-item/domain/entity/order-item';

export class AddOrderItemCommand {
    constructor(public readonly orderId: string, public readonly item: OrderItem) {}
}
