import { Injectable } from '@nestjs/common';
import { IGetOrderDetailsUseCase } from './get-order-detail.interface';
import { Order } from 'src/checkout/core/entity/order/order';
import { OrderNotFoundException } from 'src/checkout/core/exceptions/order/order-not-found.exception';
import { IOrderRepository } from 'src/checkout/core/repository/order.repository';

@Injectable()
export class GetOrderDetailsUseCase implements IGetOrderDetailsUseCase {
  constructor(private readonly repository: IOrderRepository) {}

  async execute(orderId: string): Promise<Order> {
    const order = await this.repository.findByIdWithRelations(orderId);
    if (!order) {
      throw new OrderNotFoundException(orderId);
    }
    return order;
  }
}
