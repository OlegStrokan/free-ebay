import { DataSource } from 'typeorm';
import { config } from 'dotenv';
import { ProductDb } from 'src/product/infrastructure/entity/product.entity';

config();

export default new DataSource({
  type: 'postgres',
  host: process.env.DB_HOST || 'localhost',
  port: parseInt(process.env.DB_PORT, 10) || 6434,
  username: process.env.DB_USER || 'stroka01',
  password: process.env.DB_PASSWORD || 'test',
  database: process.env.DB_NAME || 'test_exchange_db',
  entities: [ProductDb],
  logging: process.env.NODE_ENV === 'development',
  migrations: [`${__dirname}/migrations/*{.ts,.js}`],
  migrationsTableName: 'migrations',
});
