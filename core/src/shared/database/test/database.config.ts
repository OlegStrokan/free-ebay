import { registerAs } from '@nestjs/config';
import { ProductDb } from 'src/product/infrastructure/entity/product.entity';

export const TestDbConfig = registerAs('test_exchange', () => ({
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
}));
