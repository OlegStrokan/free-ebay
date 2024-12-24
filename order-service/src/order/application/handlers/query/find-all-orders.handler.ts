import { IQueryHandler, QueryHandler } from '@nestjs/cqrs';
import { Inject } from '@nestjs/common';
import { FindOrdersQuery } from '../../query/find-orders.query';
import { Order } from 'src/order/domain/order';
import { OrderQueryRepository } from 'src/order/infrastructure/repository/order-query.repository';

@QueryHandler(FindOrdersQuery)
export class FindAllOrdersHandler implements IQueryHandler<FindOrdersQuery, Order[]> {
    constructor(@Inject(OrderQueryRepository) private orderQueryRepository: OrderQueryRepository) {}

    async execute(query: FindOrdersQuery): Promise<Order[]> {
        return await this.orderQueryRepository.findAllWithItemsAndParcelsByCustomerId(query.filter.customerId);
    }
}
