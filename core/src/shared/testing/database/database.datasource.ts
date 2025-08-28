import { DataSource } from 'typeorm';
import { join } from 'path';
import { ConfigService } from '@nestjs/config';
import { glob } from 'glob';
// @non-required-fix: I will hate myself for this hack in the future but for now i am fine
import * as dotenv from 'dotenv';

const envFile = process.env.NODE_ENV === 'dev' ? '.dev.env' : '.prod.env';
dotenv.config({ path: envFile });

const configService = new ConfigService();

export const AppDataSource = new DataSource({
  type: 'postgres',
  host: configService.get<string>('POSTGRES_HOST'),
  port: configService.get<number>('POSTGRES_PORT'),
  username: configService.get<string>('POSTGRES_USER'),
  password: configService.get<string>('POSTGRES_PASSWORD'),
  database: configService.get<string>('POSTGRES_DB'),
  url: configService.get<string>('DATABASE_URL'),
  entities: glob.sync(
    join(__dirname, '..', '..', '..', '**', '*.entity.{ts,js}'),
  ),
  migrations: [join(__dirname, 'migrations', '*{.ts,.js}')],
  ssl:
    configService.get<string>('NODE_ENV') === 'dev'
      ? false
      : { rejectUnauthorized: false },
  migrationsTableName: 'migrations',
  synchronize: false,
  logging: configService.get<string>('NODE_ENV') === 'prod',
});
