import { registerAs } from '@nestjs/config';
import { CategoryDb } from 'src/catalog/infrastructure/entity/category';
import { CartItemDb } from 'src/checkout/infrastructure/entity/cart-item.entity';
import { CartDb } from 'src/checkout/infrastructure/entity/cart.entity';
import { OrderItemDb } from 'src/checkout/infrastructure/entity/order-item.entity';
import { OrderDb } from 'src/checkout/infrastructure/entity/order.entity';
import { PaymentDb } from 'src/checkout/infrastructure/entity/payment.entity';
import { ShipmentDb } from 'src/checkout/infrastructure/entity/shipment.entity';
import { ProductDb } from 'src/product/infrastructure/entity/product.entity';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';

export const TestDbConfig = registerAs('test_exchange', () => ({
  type: 'postgres',
  host: process.env.DB_HOST || 'localhost',
  port: Number(process.env.DB_PORT) || 6434,
  username: process.env.DB_USER || 'stroka01',
  password: process.env.DB_PASSWORD || 'test',
  database: process.env.DB_NAME || 'test_exchange_db',
  entities: [
    ProductDb,
    UserDb,
    CategoryDb,
    CartDb,
    CartItemDb,
    PaymentDb,
    ShipmentDb,
    OrderDb,
    OrderItemDb,
  ],
  logging: process.env.NODE_ENV === 'development',
  migrations: [`${__dirname}/migrations/*{.ts,.js}`],
  migrationsTableName: 'migrations',
}));
