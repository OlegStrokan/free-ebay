import { registerAs } from '@nestjs/config';
import { CategoryDb } from 'src/catalog/infrastructure/entity/category';
import { ProductDb } from 'src/product/infrastructure/entity/product.entity';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';

export const DbConfig = registerAs('exchange', () => ({
  type: 'postgres',
  host: process.env.DB_HOST || 'localhost',
  port: Number(process.env.DB_PORT) || 6433,
  username: process.env.DB_USER || 'stroka01',
  password: process.env.DB_PASSWORD || 'admin',
  database: process.env.DB_NAME || 'exchange',
  entities: [ProductDb, UserDb, CategoryDb],
  logging: process.env.NODE_ENV === 'development',
  migrations: [`${__dirname}/migrations/*{.ts,.js}`],
  migrationsTableName: 'migrations',
}));
