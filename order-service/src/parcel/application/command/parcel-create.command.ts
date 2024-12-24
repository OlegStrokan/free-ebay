import { CreateOrderItemCommand } from 'src/order-item/application/command/create-order-item.command';
import { Dimension } from 'src/shipping-cost/domain/shipping-cost';

export class ParcelCreateCommand {
    constructor(
        public readonly weight: number,
        public readonly dimensions: Dimension,
        public readonly orderId: string,
        public readonly items?: CreateOrderItemCommand[]
    ) {}
}
