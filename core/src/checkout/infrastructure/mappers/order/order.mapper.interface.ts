import { Order, OrderData } from 'src/checkout/core/entity/order/order';
import { OrderDb } from '../../entity/order.entity';

export abstract class IOrderMapper {
  abstract toDb(domain: Order): OrderDb;
  abstract toDomain(db: OrderDb): Order;
  abstract toClient(domain: Order): OrderData;
}
