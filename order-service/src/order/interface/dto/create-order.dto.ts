import { OrderItemDto } from 'src/order-item/interface/dto/order-item.dto';

export class CreateOrderDto {
    customerId: string;
    totalAmount: number;
    orderItems?: OrderItemDto[];
}
