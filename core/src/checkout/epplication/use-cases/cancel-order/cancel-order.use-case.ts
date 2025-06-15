import { Injectable } from '@nestjs/common';
import { ICancelOrderUseCase } from './cancel-order.interface';
import { IOrderRepository } from 'src/checkout/core/repository/order.repository';
import { OrderNotFoundException } from 'src/checkout/core/exceptions/order/order-not-found.exception';
import { Order } from 'src/checkout/core/entity/order/order';

@Injectable()
export class CancelOrderUseCase implements ICancelOrderUseCase {
  constructor(private readonly orderRepository: IOrderRepository) {}

  async execute(orderId: string): Promise<Order> {
    const order = await this.orderRepository.findByIdWithRelations(orderId);
    if (!order) {
      throw new OrderNotFoundException(orderId);
    }

    const cancelledOrder = order.markAsCancelled();
    return await this.orderRepository.save(cancelledOrder);
  }
}
