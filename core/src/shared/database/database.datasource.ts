import { DataSource } from 'typeorm';
import { config } from 'dotenv';
import { ProductDb } from 'src/product/infrastructure/entity/product.entity';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';
import { CategoryDb } from 'src/catalog/infrastructure/entity/category';
import { CartDb } from 'src/checkout/infrastructure/entity/cart.entity';
import { OrderDb } from 'src/checkout/infrastructure/entity/order.entity';
import { PaymentDb } from 'src/checkout/infrastructure/entity/payment.entity';
import { ShipmentDb } from 'src/checkout/infrastructure/entity/shipment.entity';
import { CartItemDb } from 'src/checkout/infrastructure/entity/cart-item.entity';
import { OrderItemDb } from 'src/checkout/infrastructure/entity/order-item.entity';

config();

export default new DataSource({
  type: 'postgres',
  host: process.env.DB_HOST || 'localhost',
  port: Number(process.env.DB_PORT) || 6433,
  username: process.env.DB_USER || 'stroka01',
  password: process.env.DB_PASSWORD || 'admin',
  database: process.env.DB_NAME || 'exchange',
  entities: [
    ProductDb,
    UserDb,
    CategoryDb,
    OrderDb,
    CartDb,
    CartItemDb,
    OrderItemDb,
    PaymentDb,
    ShipmentDb,
  ],
  logging: process.env.NODE_ENV === 'development',
  migrations: [`${__dirname}/migrations/*{.ts,.js}`],
  migrationsTableName: 'migrations',
});
