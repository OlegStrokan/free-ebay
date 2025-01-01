// src/infrastructure/repositories/order.repository.ts
import { Injectable } from '@nestjs/common';
import { Repository } from 'typeorm';
import { InjectRepository } from '@nestjs/typeorm';
import { OrderDb } from '../entity/order.entity';
import { Order } from 'src/checkout/core/entity/order';
import { IOrderRepository } from 'src/checkout/core/repository/order.repository';

@Injectable()
export class OrderRepository implements IOrderRepository {
  constructor(
    @InjectRepository(OrderDb)
    private readonly orderRepository: Repository<OrderDb>,
  ) {}

  async createOrder(orderData: Partial<Order>): Promise<Order> {
    const order = this.orderRepository.create(orderData);
    return this.orderRepository.save(order);
  }

  async findById(orderId: string): Promise<Order> {
    return this.orderRepository.findOne(orderId);
  }

  async cancelOrder(orderId: string): Promise<Order> {
    const order = await this.findById(orderId);
    order.status = 'Cancelled'; // Business logic: changing status to 'Cancelled'
    return this.orderRepository.save(order);
  }

  async findAll(): Promise<Order[]> {
    return this.orderRepository.find();
  }
}
