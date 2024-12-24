import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { ShippingCostCommand } from '../entity/shipping-cost-command.entity';
import { ShippingCostCommandMapper } from '../mapper/shipping-cost-command.mapper';
import { ShippingCost } from 'src/shipping-cost/domain/shipping-cost';
import { IShippingCostCommandRepository } from 'src/shipping-cost/domain/shipping-cost-command.repository';
import { ShippingCostCreateDate } from 'src/shipping-cost/domain/shipping-cost';

@Injectable()
export class ShippingCostCommandRepository implements IShippingCostCommandRepository {
    constructor(
        @InjectRepository(ShippingCostCommand)
        private readonly shippingCostCommandRepository: Repository<ShippingCostCommand>
    ) {}

    async create(data: ShippingCostCreateDate): Promise<void> {
        const shippingCostEntity = ShippingCostCommandMapper.toEntity(data);
        await this.shippingCostCommandRepository.save(shippingCostEntity);
    }

    async findByOrderId(orderId: string): Promise<ShippingCost | undefined> {
        const shippingCost = await this.shippingCostCommandRepository.findOne({ where: { orderId } });
        return ShippingCostCommandMapper.toDomain(shippingCost);
    }
}
