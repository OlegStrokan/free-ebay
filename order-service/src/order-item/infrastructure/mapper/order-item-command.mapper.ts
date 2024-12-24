import { OrderItem } from 'src/order-item/domain/entity/order-item';
import { OrderItemDto } from 'src/order-item/interface/dto/order-item.dto';
import { OrderItemCommand } from '../entity/order-item-command.entity';

export class OrderItemCommandMapper {
    static toDomain(itemCommand: OrderItemCommand): OrderItem {
        return new OrderItem({
            id: itemCommand.id,
            productId: itemCommand.productId,
            quantity: itemCommand.quantity,
            price: itemCommand.price,
            weight: itemCommand.weight,
            createdAt: itemCommand.createdAt,
            updatedAt: itemCommand.updatedAt,
        });
    }

    static toClient(item: OrderItem): OrderItemDto {
        return {
            ...item.data,
        };
    }

    static toEntity(item: OrderItem): OrderItemCommand {
        const itemCommand = new OrderItemCommand();
        itemCommand.id = item.id;
        itemCommand.productId = item.productId;
        itemCommand.quantity = item.quantity;
        itemCommand.price = item.price;
        itemCommand.weight = item.weight;
        return itemCommand;
    }

    static;
}
