import { TypeOrmModule } from '@nestjs/typeorm';
import { ProductModule } from 'src/product/product.module';
import { UserModule } from 'src/user/user.module';
import { CartItemDb } from './infrastructure/entity/cart-item.entity';
import { CartDb } from './infrastructure/entity/cart.entity';
import { OrderItemDb } from './infrastructure/entity/order-item.entity';
import { OrderDb } from './infrastructure/entity/order.entity';
import { PaymentDb } from './infrastructure/entity/payment.entity';
import { ShipmentDb } from './infrastructure/entity/shipment.entity';
import { CheckoutController } from './interface/checkout.controller';
import { Inject, Module } from '@nestjs/common';
import { checkoutProviders } from './checkout.providers';
import { ClientKafka, ClientsModule, Transport } from '@nestjs/microservices';

@Module({
  imports: [
    ClientsModule.register([
      {
        name: 'KAFKA_PRODUCER',
        transport: Transport.KAFKA,
        options: {
          client: {
            clientId: 'core-app',
            brokers: ['localhost:9092'],
          },
          consumer: {
            heartbeatInterval: 10000,
            sessionTimeout: 60000,
            groupId: 'core-app',
          },
        },
      },
    ]),
    TypeOrmModule.forFeature([
      CartDb,
      CartItemDb,
      PaymentDb,
      ShipmentDb,
      OrderDb,
      OrderItemDb,
    ]),
    UserModule,
    ProductModule,
  ],
  exports: [],
  providers: [...checkoutProviders],
  controllers: [CheckoutController],
})
export class CheckoutModule {
  constructor(@Inject('KAFKA_PRODUCER') private client: ClientKafka) {}

  async onModuleInit() {
    this.client.subscribeToResponseOf('payment');
    await this.client.connect();
  }
}
