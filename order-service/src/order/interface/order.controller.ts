import { Body, Controller, Get, Headers, Param, Patch, Post, Query } from '@nestjs/common';
import { CommandBus, EventBus, QueryBus } from '@nestjs/cqrs';
import { CreateOrderDto } from './dto/create-order.dto';
import { CreateOrderCommand } from '../application/command/create-order.command';
import { AuthorizedHeader } from 'src/libs/auth';
import { CompleteOrderCommand } from '../application/command/complete-order.command';
import { CancelOrderCommand } from '../application/command/cancel-order.command';
import { ShipOrderCommand } from '../application/command/ship-order.command';
import { FindOrdersQuery } from '../application/query/find-orders.query';
import { FindOrderByIdResponseDto } from './response-dto/find-order-response.dto';
import { FindOrderByIdQuery } from '../application/query/find-order-by-id.query';
import { Order } from '../domain/order';
import { OrderCommandMapper } from '../infrastructure/mapper/order-command.mapper';
import { OrderDto } from './dto/order.dto';
import { ShipOrderDto } from './dto/ship-order.dto';

@Controller('orders')
export class OrderController {
    constructor(
        private readonly commandBus: CommandBus,
        private readonly queryBus: QueryBus,
        private readonly eventBus: EventBus
    ) {}

    @Post()
    public async createOrder(@Body() dto: CreateOrderDto): Promise<void> {
        const command = new CreateOrderCommand(dto.customerId, dto.totalAmount, dto.orderItems);
        await this.commandBus.execute(command);
    }

    @Patch(':orderId')
    public async completeOrder(@Headers() header: AuthorizedHeader, @Param() param: string): Promise<void> {
        const command = new CompleteOrderCommand(param);
        await this.commandBus.execute(command);
    }

    @Patch(':orderId/cancel')
    public async cancelOrder(@Headers() header: AuthorizedHeader, @Param() param: { orderId: string }): Promise<void> {
        const command = new CancelOrderCommand(param.orderId);
        await this.commandBus.execute(command);
    }

    @Patch(':order/ship')
    public async shipOrder(@Headers() header: AuthorizedHeader, @Param() param: string, @Body() dto: ShipOrderDto) {
        const command = new ShipOrderCommand(param, dto.trackingNumber, dto.deliveryDate);
        await this.commandBus.execute(command);
    }

    @Get()
    public async findOrders(@Query() query: { customerId?: string }): Promise<OrderDto[]> {
        const queryInstanse = new FindOrdersQuery(query);
        const orders = await this.queryBus.execute<FindOrdersQuery, Order[]>(queryInstanse);
        return orders.map((order) => OrderCommandMapper.toClient(order));
    }

    @Get(':orderId')
    public async findOrderById(@Param() param: string): Promise<FindOrderByIdResponseDto> {
        const query = new FindOrderByIdQuery(param);
        return this.queryBus.execute(query);
    }
}
