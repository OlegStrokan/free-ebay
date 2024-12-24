import { Injectable } from '@nestjs/common';
import { Repository } from 'typeorm';
import { InjectRepository } from '@nestjs/typeorm';
import { IOrderCommandRepository } from 'src/order/domain/order-command.repository';
import { OrderCommand } from '../entity/order-command.entity';
import { OrderData } from 'src/order/domain/order';

@Injectable()
export class OrderCommandRepository implements IOrderCommandRepository {
    constructor(
        @InjectRepository(OrderCommand, 'commandConnection')
        private readonly orderRepository: Repository<OrderCommand>
    ) {}

    public async insertOne(order: OrderData): Promise<void> {
        await this.orderRepository.save(order);
    }

    public async updateOne(order: OrderData): Promise<void> {
        await this.orderRepository.save(order);
    }

    public async deleteOne(orderId: string): Promise<void> {
        await this.orderRepository.delete(orderId);
    }
}
