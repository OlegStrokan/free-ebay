import { Dimension } from 'src/shipping-cost/domain/shipping-cost';

export class ParcelDto {
    id: string;
    trackingNumber: string;
    weight: number;
    dimensions: Dimension;
    orderId: string;
    createdAt: Date;
    updatedAt: Date;
}
