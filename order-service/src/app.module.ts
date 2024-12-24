import { Module } from '@nestjs/common';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import { DbCommandConfig } from './libs/database/command/database-command.config';
import { DbQueryConfig } from './libs/database/query/database-query.config';
import { KafkaModule } from './libs/kafka/kafka.module';
import { OrderModule } from './order/order.module';
import { OrderItemCommand } from './order-item/infrastructure/entity/order-item-command.entity';
import { OrderItemQuery } from './order-item/infrastructure/entity/order-item-query.entity';
import { OrderCommand } from './order/infrastructure/entity/order-command.entity';
import { OrderQuery } from './order/infrastructure/entity/order-query.entity';
import { ParcelCommand } from './parcel/infrastructure/entity/parcel-command.entity';
import { ParcelQuery } from './parcel/infrastructure/entity/parcel-query.entity';
import { RepaymentPreferencesCommand } from './repayment-preferences/infrastructure/entity/repayment-preferences-command.entity';
import { RepaymentPreferencesQuery } from './repayment-preferences/infrastructure/entity/repayment-preferences-query.entity';
import { ShippingCostCommand } from './shipping-cost/infrastructure/entity/shipping-cost-command.entity';
import { ShippingCostQuery } from './shipping-cost/infrastructure/entity/shipping-cost-query.entity';

@Module({
    imports: [
        ConfigModule.forRoot({
            isGlobal: true,
            cache: true,
            load: [DbCommandConfig, DbQueryConfig],
            envFilePath: `.env`,
        }),
        TypeOrmModule.forRootAsync({
            name: 'commandConnection',
            imports: [ConfigModule],
            useFactory: (configService: ConfigService) => ({
                ...configService.get('commandConnection'),
            }),
            inject: [ConfigService],
        }),
        TypeOrmModule.forRootAsync({
            name: 'queryConnection',
            imports: [ConfigModule],
            useFactory: (configService: ConfigService) => ({
                ...configService.get('queryConnection'),
            }),
            inject: [ConfigService],
        }),
        TypeOrmModule.forFeature(
            [OrderCommand, OrderItemCommand, ParcelCommand, RepaymentPreferencesCommand, ShippingCostCommand],
            'commandConnection'
        ),
        TypeOrmModule.forFeature(
            [OrderQuery, OrderItemQuery, ParcelQuery, RepaymentPreferencesQuery, ShippingCostQuery],
            'queryConnection'
        ),
        KafkaModule,
        OrderModule,
    ],
})
export class AppModule {}
