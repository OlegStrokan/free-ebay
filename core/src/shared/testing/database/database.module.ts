import { ConfigService } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import { Module } from '@nestjs/common';
import { AppDataSource } from './database.datasource';

@Module({
  imports: [
    TypeOrmModule.forRootAsync({
      useFactory: async () => AppDataSource.options,
      inject: [ConfigService],
    }),
  ],
  exports: [TypeOrmModule],
})
export class DatabaseModule {}
