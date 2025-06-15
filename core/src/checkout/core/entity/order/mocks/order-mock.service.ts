import { Injectable } from '@nestjs/common';
import { Money } from 'src/shared/types/money';
import { IOrderMockService } from './order-mock.interface';
import { IOrderRepository } from 'src/checkout/core/repository/order.repository';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { faker } from '@faker-js/faker';
import { Order } from '../order';
import { OrderStatus } from '../order';
import { OrderData } from '../order';
import { CreateOrderDto } from 'src/checkout/interface/dtos/create-order.dto';
import { PaymentMethod } from '../../payment/payment';
import { UserData } from 'src/user/core/entity/user';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { IOrderItemMockService } from '../../order-item/mocks/order-item-mock.interface';
import { OrderItemData } from '../../order-item/order-item';

@Injectable()
export class OrderMockService implements IOrderMockService {
  constructor(
    private readonly orderRepository: IOrderRepository,
    private readonly userMockService: IUserMockService,
    private readonly orderItemMockService: IOrderItemMockService,
  ) {}

  getOneToCreate(overrides: Partial<CreateOrderDto> = {}): CreateOrderDto {
    return {
      cartId: overrides?.cartId ?? generateUlid(),
      paymentMethod:
        overrides?.paymentMethod ?? faker.helpers.enumValue(PaymentMethod),
      shippingAddress: overrides?.shippingAddress ?? faker.location.street(),
    };
  }

  getOne(overrides: Partial<OrderData> = {}): Order {
    const orderData: OrderData = {
      id: overrides?.id ?? generateUlid(),
      userId: overrides?.userId ?? generateUlid(),
      status: overrides?.status ?? OrderStatus.Shipped,
      items: overrides?.items ?? [],
      totalPrice:
        overrides?.totalPrice ?? new Money(faker.number.int(1000), 'USD', 100),
      createdAt: overrides?.createdAt ?? new Date(),
      updatedAt: overrides?.updatedAt ?? new Date(),
      shipment: overrides?.shipment,
      payment: overrides?.payment,
    };

    return new Order(orderData);
  }

  async createOne(overrides: Partial<OrderData> = {}): Promise<Order> {
    const order = this.getOne(overrides);
    return await this.orderRepository.save(order);
  }

  async createOneWithDependencies(
    orderOverrides: Partial<OrderData> = {},
    userOverrides: Partial<UserData> = {},
    orderItemOverrides: Partial<OrderItemData>[] = [],
    orderItemsCount = 1,
  ): Promise<Order> {
    await this.userMockService.createOne(userOverrides);
    const order = this.getOne({
      ...orderOverrides,
    });

    const items = this.orderItemMockService
      .getMany(orderItemsCount, orderItemOverrides)
      .map((item) => {
        item.data.id = order.id;
        return item.data;
      });

    order.data.items = items;

    return await this.orderRepository.save(order);
  }
}
