import { CommandHandler, EventBus, ICommandHandler } from '@nestjs/cqrs';
import { CancelOrderCommand } from '../../command/cancel-order.command';
import { Inject, NotFoundException } from '@nestjs/common';
import { IOrderCommandRepository } from 'src/order/domain/order-command.repository';
import { OrderCancelledEvent } from 'src/order/domain/event/order-canceled.event';
import { OrderQueryRepository } from 'src/order/infrastructure/repository/order-query.repository';
import { OrderCommandRepository } from 'src/order/infrastructure/repository/order-command.repository';

@CommandHandler(CancelOrderCommand)
export class CancelOrderHandler implements ICommandHandler<CancelOrderCommand, void> {
    constructor(
        @Inject(OrderCommandRepository) private orderCommandRepository: IOrderCommandRepository,
        @Inject(OrderQueryRepository) private orderQueryRepository: OrderQueryRepository,
        private readonly eventBus: EventBus
    ) {}

    async execute(command: CancelOrderCommand): Promise<void> {
        console.log(command);
        const order = await this.orderQueryRepository.findById(command.id);
        if (!order) {
            throw new NotFoundException('Order not found');
        }
        const canceledOrder = order.cancel();
        await this.orderCommandRepository.updateOne(canceledOrder.data);

        this.eventBus.publish(new OrderCancelledEvent(order.id, new Date()));
    }
}
