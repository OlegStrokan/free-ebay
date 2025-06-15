import { Injectable } from '@nestjs/common';
import { IGetAllUserOrdersUseCase } from './get-all-user-orders.interface';
import { Order } from 'src/checkout/core/entity/order/order';
import { IOrderRepository } from 'src/checkout/core/repository/order.repository';

@Injectable()
export class GetAllUserOrdersUseCase implements IGetAllUserOrdersUseCase {
  constructor(private readonly repository: IOrderRepository) {}

  async execute(userId: string): Promise<Order[]> {
    return await this.repository.findAllByUserId(userId);
  }
}
