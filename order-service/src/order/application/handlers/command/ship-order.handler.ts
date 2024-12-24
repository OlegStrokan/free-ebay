import { CommandHandler, ICommandHandler } from '@nestjs/cqrs';
import { ShipOrderCommand } from '../../command/ship-order.command';
import { Inject, NotFoundException } from '@nestjs/common';
import { IOrderCommandRepository } from 'src/order/domain/order-command.repository';
import { EventBus } from '@nestjs/cqrs';
import { OrderShippedEvent } from 'src/order/domain/event/order-shipped.event';
import { OrderCommandRepository } from 'src/order/infrastructure/repository/order-command.repository';
import { OrderQueryRepository } from 'src/order/infrastructure/repository/order-query.repository';

@CommandHandler(ShipOrderCommand)
export class ShipOrderHandler implements ICommandHandler<ShipOrderCommand, void> {
    constructor(
        @Inject(OrderCommandRepository) private orderRepository: IOrderCommandRepository,
        @Inject(OrderQueryRepository) private orderQueryRepository: OrderQueryRepository,
        private readonly eventBus: EventBus
    ) {}

    async execute(command: ShipOrderCommand): Promise<void> {
        const order = await this.orderQueryRepository.findById(command.id);
        if (!order) {
            throw new NotFoundException('Order not found');
        }
        const shippedOrder = order.ship(command.trackingNumber, command.deliveryDate);
        await this.orderRepository.updateOne(shippedOrder.data);

        this.eventBus.publish(
            new OrderShippedEvent(order.id, command.trackingNumber, command.deliveryDate, command.deliveryDate)
        );
    }
}
