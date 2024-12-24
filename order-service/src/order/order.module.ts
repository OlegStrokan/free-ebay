import { TypeOrmModule } from '@nestjs/typeorm';
import { OrderCommandRepository } from './infrastructure/repository/order-command.repository';
import { Logger, Module } from '@nestjs/common';
import { CreateOrderHandler } from './application/handlers/command/create-order.handler';
import { OrderController } from './interface/order.controller';
import { CqrsModule } from '@nestjs/cqrs';
import { OrderItemCommand } from 'src/order-item/infrastructure/entity/order-item-command.entity';
import { OrderItemQuery } from 'src/order-item/infrastructure/entity/order-item-query.entity';
import { OrderItemCommandRepository } from 'src/order-item/infrastructure/repository/order-item-command.repository';
import { OrderItemQueryRepository } from 'src/order-item/infrastructure/repository/order-item-query.repository';
import { ParcelCommand } from 'src/parcel/infrastructure/entity/parcel-command.entity';
import { ParcelQuery } from 'src/parcel/infrastructure/entity/parcel-query.entity';
import { ParcelCommandRepository } from 'src/parcel/infrastructure/repository/parcel-command.repository';
import { ParcelQueryRepository } from 'src/parcel/infrastructure/repository/parcel-query.repository';
import { ShippingCostCommand } from 'src/shipping-cost/infrastructure/entity/shipping-cost-command.entity';
import { ShippingCostQuery } from 'src/shipping-cost/infrastructure/entity/shipping-cost-query.entity';
import { CancelOrderHandler } from './application/handlers/command/cancel-order.handler';
import { ShipOrderHandler } from './application/handlers/command/ship-order.handler';
import { FindAllOrdersHandler } from './application/handlers/query/find-all-orders.handler';
import { OrderCancelledHandler } from './application/handlers/query/order-canceled.handler';
import { OrderCompletedHandler } from './application/handlers/query/order-completed.handler';
import { OrderCreatedHandler } from './application/handlers/query/order-created.handler';
import { OrderShippeHandler } from './application/handlers/query/order-shipped.handler';
import { OrderCommand } from './infrastructure/entity/order-command.entity';
import { OrderQuery } from './infrastructure/entity/order-query.entity';
import { OrderQueryRepository } from './infrastructure/repository/order-query.repository';

@Module({
    imports: [
        TypeOrmModule.forFeature(
            [OrderCommand, OrderItemCommand, ParcelCommand, ShippingCostCommand],
            'commandConnection'
        ),
        TypeOrmModule.forFeature([OrderQuery, OrderItemQuery, ParcelQuery, ShippingCostQuery], 'queryConnection'),
        CqrsModule,
    ],
    controllers: [OrderController],
    providers: [
        Logger,
        OrderCommandRepository,
        OrderQueryRepository,
        ParcelQueryRepository,
        OrderItemQueryRepository,
        OrderItemCommandRepository,
        ParcelCommandRepository,
        CreateOrderHandler,
        CancelOrderHandler,
        ShipOrderHandler,
        OrderCreatedHandler,
        OrderCancelledHandler,
        OrderShippeHandler,
        OrderCompletedHandler,
        FindAllOrdersHandler,
    ],
    exports: [
        OrderCommandRepository,
        OrderQueryRepository,
        ParcelQueryRepository,
        OrderItemQueryRepository,
        OrderItemCommandRepository,
        ParcelCommandRepository,
        CreateOrderHandler,
        CancelOrderHandler,
        ShipOrderHandler,
        OrderCreatedHandler,
        OrderCancelledHandler,
        OrderShippeHandler,
        OrderCompletedHandler,
        FindAllOrdersHandler,
    ],
})
export class OrderModule {}
