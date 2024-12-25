import { ConfigModule, ConfigService } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import { DbConfig } from './shared/database/database.config';
import { Module } from '@nestjs/common';
import { ProductModule } from './product/product.module';
import { ProductDb } from './product/infrastructure/entity/product.entity';

@Module({
  imports: [
    ConfigModule.forRoot({
      isGlobal: true,
      cache: true,
      load: [DbConfig],
      envFilePath: `.env`,
    }),
    TypeOrmModule.forRootAsync({
      imports: [ConfigModule],
      useFactory: (configService: ConfigService) => ({
        ...configService.get('exchange'),
      }),
      inject: [ConfigService],
    }),
    TypeOrmModule.forFeature([ProductDb]),
    ProductModule,
  ],
  exports: [],
  providers: [],
})
export class AppModule {}
