import { ParcelCreateCommand } from 'src/parcel/application/command/parcel-create.command';
import { Dimension } from 'src/shipping-cost/domain/shipping-cost';
import { ShippingOptions } from 'src/shipping-cost/domain/shipping-cost';

export class CreateShippingCostCommand {
    constructor(
        public readonly orderId: string,
        public readonly weight: number,
        public readonly dimensions: Dimension,
        public readonly shippingOptions: ShippingOptions,
        public readonly parcels?: ParcelCreateCommand[]
    ) {}
}
