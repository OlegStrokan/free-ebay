import { Injectable } from '@nestjs/common';
import { Repository } from 'typeorm';
import { InjectRepository } from '@nestjs/typeorm';
import { OrderDb } from '../entity/order.entity';
import { IOrderRepository } from 'src/checkout/core/repository/order.repository';
import { Order } from 'src/checkout/core/entity/order/order';
import { IOrderMapper } from '../mappers/order/order.mapper.interface';
import { OrderItemDb } from '../entity/order-item.entity';
import { IClearableRepository } from 'src/shared/types/clearable';

@Injectable()
export class OrderRepository implements IOrderRepository, IClearableRepository {
  constructor(
    @InjectRepository(OrderDb)
    private readonly orderRepository: Repository<OrderDb>,
    @InjectRepository(OrderDb)
    private readonly orderItemRepository: Repository<OrderItemDb>,

    private readonly mapper: IOrderMapper,
  ) {}

  async save(orderData: Order): Promise<Order> {
    const order = this.mapper.toDb(orderData);
    const savedOrder = await this.orderRepository.save(order);
    return this.mapper.toDomain(savedOrder);
  }

  async update(cart: Order): Promise<Order> {
    const dbOrder = this.mapper.toDb(cart);

    if (dbOrder.items.length === 0) {
      await this.orderItemRepository.delete({ order: dbOrder });
    }

    const savedCart = await this.orderRepository.save(dbOrder);
    return this.mapper.toDomain(savedCart);
  }

  async findById(orderId: string): Promise<Order | null> {
    const order = await this.orderRepository.findOneBy({ id: orderId });
    return order ? this.mapper.toDomain(order) : null;
  }

  async findAllByUserId(userId: string): Promise<Order[]> {
    const orders = await this.orderRepository.find({
      where: { user: { id: userId } },
      relations: ['user'],
    });
    return orders.map((order) => this.mapper.toDomain(order));
  }

  async findByIdWithRelations(orderId: string): Promise<Order | null> {
    const order = await this.orderRepository.findOne({
      where: { id: orderId },
      relations: ['items', 'user'],
    });
    return order ? this.mapper.toDomain(order) : null;
  }

  async findAll(): Promise<Order[]> {
    const orders = await this.orderRepository.find();
    return orders.map((order) => this.mapper.toDomain(order));
  }

  async clear(): Promise<void> {
    await this.orderItemRepository.query(`DELETE FROM "orders"`);
  }
}
