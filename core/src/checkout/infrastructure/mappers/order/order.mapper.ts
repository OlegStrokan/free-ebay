import { Money } from 'src/shared/types/money';
import { MoneyMapper } from 'src/product/infrastructure/mappers/money/money.mapper';
import { Inject } from '@nestjs/common';
import { IMoneyMapper } from 'src/product/infrastructure/mappers/money/money.mapper.interface';
import { Order, OrderData } from 'src/checkout/core/entity/order/order';
import { OrderDb } from '../../entity/order.entity';
import { OrderItemDb } from '../../entity/order-item.entity';
import { IOrderMapper } from './order.mapper.interface';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';

export class OrderMapper implements IOrderMapper<OrderData, Order, OrderDb> {
  constructor(
    @Inject(MoneyMapper)
    private readonly moneyMapper: IMoneyMapper,
  ) {}

  toDomain(orderDB: OrderDb): Order {
    const orderData: OrderData = {
      id: orderDB.id,
      status: orderDB.status,
      userId: orderDB.user?.id ?? null,
      items: orderDB?.items
        ? orderDB.items.map((orderItem) => ({
            id: orderItem.id,
            productId: orderItem.productId,
            orderId: orderItem.orderId,
            quantity: orderItem.quantity,
            createdAt: orderItem.createdAt,
            updatedAt: orderItem.updatedAt,
            priceAtPurchase:
              this.moneyMapper.toDomain(orderItem.priceAtPurchase) ??
              Money.getDefaultMoney(),
          }))
        : [],
      totalPrice:
        this.moneyMapper.toDomain(orderDB.totalPrice) ??
        Money.getDefaultMoney(),
      createdAt: orderDB.createdAt,
      updatedAt: orderDB.updatedAt,
    };

    return new Order(orderData);
  }

  toDb(order: Order): OrderDb {
    const orderDb = new OrderDb();
    orderDb.id = order.id;
    orderDb.status = order.status;
    orderDb.user = { id: order.userId } as UserDb;
    orderDb.items = order.items.map((item) => {
      const orderItem = new OrderItemDb();
      orderItem.id = item.id;
      orderItem.orderId = order.id;
      orderItem.quantity = item.quantity;
      orderItem.createdAt = item.createdAt;
      orderItem.productId = item.productId;
      orderItem.updatedAt = item.updatedAt;
      orderItem.priceAtPurchase =
        this.moneyMapper.toDb(item.priceAtPurchase) ?? '';
      return orderItem;
    });
    orderDb.totalPrice = JSON.stringify(order.totalPrice);
    orderDb.createdAt = order.data.createdAt;
    orderDb.updatedAt = order.data.updatedAt;

    return orderDb;
  }

  toClient(order: Order): OrderData {
    return order.data;
  }
}
