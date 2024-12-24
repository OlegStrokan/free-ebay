import { DataSource } from 'typeorm';
import { config } from 'dotenv';
import { OrderItemCommand } from 'src/order-item/infrastructure/entity/order-item-command.entity';
import { OrderCommand } from 'src/order/infrastructure/entity/order-command.entity';
import { ParcelCommand } from 'src/parcel/infrastructure/entity/parcel-command.entity';
import { RepaymentPreferencesCommand } from 'src/repayment-preferences/infrastructure/entity/repayment-preferences-command.entity';
import { ShippingCostCommand } from 'src/shipping-cost/infrastructure/entity/shipping-cost-command.entity';

config();

export default new DataSource({
    type: 'postgres',
    host: process.env.DB_HOST || 'localhost',
    port: parseInt(process.env.DB_PORT, 10) || 6433,
    username: process.env.DB_USER || 'stroka01',
    password: process.env.DB_PASSWORD || 'admin',
    database: process.env.DB_NAME || 'order_command_db',
    entities: [OrderCommand, OrderItemCommand, ParcelCommand, RepaymentPreferencesCommand, ShippingCostCommand],
    logging: process.env.NODE_ENV === 'development',
    migrations: [`${__dirname}/migrations/*{.ts,.js}`],
    migrationsTableName: 'migrations',
});
