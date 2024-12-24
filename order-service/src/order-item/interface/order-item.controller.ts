import { Body, Controller, Get, Headers, Param, Patch, Post, Query } from '@nestjs/common';
import { CommandBus, EventBus, QueryBus } from '@nestjs/cqrs';
import { AuthorizedHeader } from 'src/libs/auth';
import { CancelOrderCommand } from 'src/order/application/command/cancel-order.command';
import { CompleteOrderCommand } from 'src/order/application/command/complete-order.command';
import { CreateOrderCommand } from 'src/order/application/command/create-order.command';
import { ShipOrderCommand } from 'src/order/application/command/ship-order.command';
import { FindOrderByIdQuery } from 'src/order/application/query/find-order-by-id.query';
import { FindOrdersQuery } from 'src/order/application/query/find-orders.query';
import { Order } from 'src/order/domain/order';
import { OrderCommandMapper } from 'src/order/infrastructure/mapper/order-command.mapper';
import { CreateOrderDto } from 'src/order/interface/dto/create-order.dto';
import { OrderDto } from 'src/order/interface/dto/order.dto';
import { ShipOrderDto } from 'src/order/interface/dto/ship-order.dto';
import { FindOrderByIdResponseDto } from 'src/order/interface/response-dto/find-order-response.dto';

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
