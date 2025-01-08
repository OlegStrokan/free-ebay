import { TypeOrmModule } from '@nestjs/typeorm';
import { MoneyMapper } from 'src/product/infrastructure/mappers/money/money.mapper';
import { ProductModule } from 'src/product/product.module';
import { UserModule } from 'src/user/user.module';
import { CartItemDb } from './infrastructure/entity/cart-item.entity';
import { CartDb } from './infrastructure/entity/cart.entity';
import { OrderItemDb } from './infrastructure/entity/order-item.entity';
import { OrderDb } from './infrastructure/entity/order.entity';
import { PaymentDb } from './infrastructure/entity/payment.entity';
import { ShipmentDb } from './infrastructure/entity/shipment.entity';
import { CheckoutController } from './interface/checkout.controller';
import { Module } from '@nestjs/common';
import { checkoutProviders } from './checkout.providers';

@Module({
  imports: [
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
  providers: [...checkoutProviders, MoneyMapper],
  controllers: [CheckoutController],
})
export class CheckoutModule {}
