import { DataSource } from 'typeorm';
import { config } from 'dotenv';
import { OrderItemQuery } from 'src/order-item/infrastructure/entity/order-item-query.entity';
import { OrderQuery } from 'src/order/infrastructure/entity/order-query.entity';
import { ParcelQuery } from 'src/parcel/infrastructure/entity/parcel-query.entity';
import { RepaymentPreferencesQuery } from 'src/repayment-preferences/infrastructure/entity/repayment-preferences-query.entity';
import { ShippingCostQuery } from 'src/shipping-cost/infrastructure/entity/shipping-cost-query.entity';

config();

export default new DataSource({
    type: 'postgres',
    host: process.env.DB_HOST || 'localhost',
    port: parseInt(process.env.DB_PORT, 10) || 5433,
    username: process.env.DB_USER || 'stroka01',
    password: process.env.DB_PASSWORD || 'admin',
    database: process.env.DB_NAME || 'order_query_db',
    entities: [OrderQuery, OrderItemQuery, ParcelQuery, RepaymentPreferencesQuery, ShippingCostQuery],
    logging: process.env.NODE_ENV === 'development',
    migrations: [`${__dirname}/migrations/*{.ts,.js}`],
    migrationsTableName: 'migrations',
});
