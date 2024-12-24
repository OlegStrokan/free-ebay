import { CreateOrderItemCommand } from 'src/order-item/application/command/create-order-item.command';

export class CreateOrderCommand {
    constructor(
        public readonly customerId: string,
        public readonly totalAmount: number,
        public readonly orderItems?: CreateOrderItemCommand[]
    ) {}
}
