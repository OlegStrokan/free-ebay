import { CommandHandler, EventBus, ICommandHandler } from '@nestjs/cqrs';
import { CreateOrderCommand } from '../../command/create-order.command';
import { Inject, Logger } from '@nestjs/common';
import { IOrderCommandRepository } from 'src/order/domain/order-command.repository';

import { OrderCreatedEvent } from 'src/order/domain/event/order-created.event';
import { OrderItem } from 'src/order-item/domain/entity/order-item';
import { Order } from 'src/order/domain/order';
import { OrderCommandRepository } from 'src/order/infrastructure/repository/order-command.repository';
import { IParcelCommandRepository } from 'src/parcel/domain/parcel-command.repository';
import { ParcelCommandRepository } from 'src/parcel/infrastructure/repository/parcel-command.repository';

@CommandHandler(CreateOrderCommand)
export class CreateOrderHandler implements ICommandHandler<CreateOrderCommand, void> {
    constructor(
        @Inject(OrderCommandRepository) private orderRepository: IOrderCommandRepository,
        @Inject(ParcelCommandRepository) private parcelRepository: IParcelCommandRepository,
        private readonly eventBus: EventBus,
        private readonly logger: Logger
    ) {}

    async execute(command: CreateOrderCommand): Promise<void> {
        let orderItems: OrderItem[] = [];
        if (command.orderItems) {
            orderItems = command.orderItems?.map((item) => {
                return OrderItem.create({ ...item });
            });
        }

        const order = Order.create({
            customerId: command.customerId,
            totalAmount: command.totalAmount,
            items: orderItems,
        });

        await this.orderRepository.insertOne(order.data);

        await this.parcelRepository.insertMany(order);

        this.eventBus.publish(
            new OrderCreatedEvent(
                order.id,
                order.customerId,
                order.totalAmount,
                order.specialInstruction,
                order.paymentMethod,
                order.deliveryAddress,
                order.createdAt,
                order.items.map((item) => item.data)
            )
        );

        this.logger.log(`Order created`);
    }
}
