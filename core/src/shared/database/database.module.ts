import { ConfigService } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import { AppDataSource } from './database.datasource';
import { Module } from '@nestjs/common';

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
