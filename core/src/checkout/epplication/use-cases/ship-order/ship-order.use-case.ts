import { Inject, Injectable } from '@nestjs/common';
import { IShipOrderUseCase } from './ship-order.interface';
import { ORDER_REPOSITORY } from '../../injection-tokens/repository.token';
import { OrderNotFoundException } from 'src/checkout/core/exceptions/order/order-not-found.exception';
import { IOrderRepository } from 'src/checkout/core/repository/order.repository';
import { Order } from 'src/checkout/core/entity/order/order';

@Injectable()
export class ShipOrderUseCase implements IShipOrderUseCase {
  constructor(
    @Inject(ORDER_REPOSITORY)
    private readonly repository: IOrderRepository,
  ) {}

  async execute(orderId: string): Promise<Order> {
    const order = await this.repository.findById(orderId);
    if (!order) {
      throw new OrderNotFoundException(orderId);
    }

    const cancelledOrder = order.markAsShipped();
    return await this.repository.save(cancelledOrder);
  }
}
