import { Inject, Injectable } from '@nestjs/common';
import { IGetAllUserOrdersUseCase } from './get-all-user-orders.interface';
import { ORDER_REPOSITORY } from '../../injection-tokens/repository.token';
import { Order } from 'src/checkout/core/entity/order/order';
import { IOrderRepository } from 'src/checkout/core/repository/order.repository';

@Injectable()
export class GetAllUserOrdersUseCase implements IGetAllUserOrdersUseCase {
  constructor(
    @Inject(ORDER_REPOSITORY)
    private readonly repository: IOrderRepository,
  ) {}

  async execute(userId: string): Promise<Order[]> {
    return await this.repository.findAllByUserId(userId);
  }
}
