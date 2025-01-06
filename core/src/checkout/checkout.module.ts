import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { CartDb } from './infrastructure/entity/cart.entity';
import { CartItemDb } from './infrastructure/entity/cart-item.entity';
import { CheckoutController } from './interface/checkout.controller';

import { CartMapper } from './infrastructure/mappers/cart/cart.mapper';
import { CartRepository } from './infrastructure/repository/cart.repository';
import {
  CART_REPOSITORY,
  ORDER_REPOSITORY,
  PAYMENT_REPOSITORY,
  SHIPMENT_REPOSITORY,
} from './epplication/injection-tokens/repository.token';
import {
  CART_MAPPER,
  ORDER_MAPPER,
  PAYMENT_MAPPER,
  SHIPMENT_MAPPER,
} from './epplication/injection-tokens/mapper.token';
import {
  CREATE_CART_USE_CASE_TOKEN,
  ADD_TO_CART_USE_CASE_TOKEN,
  RETRIEVE_CART_USE_CASE_TOKEN,
  CLEAR_CART_USE_CASE_TOKEN,
  CREATE_ORDER_USE_CASE_TOKEN,
  GET_ORDER_DETAILS_USE_CASE_TOKEN,
  GET_ALL_ORDERS_USE_CASE_TOKEN,
  CANCEL_ORDER_USE_CASE_TOKEN,
  SHIP_ORDER_USE_CASE_TOKEN,
  PROCEED_PAYMENT_USE_CASE_TOKEN,
  CHECK_PAYMENT_STATUS_USE_CASE_TOKEN,
  REMOVE_FROM_CART_USE_CASE_TOKEN,
} from './epplication/injection-tokens/use-case.token';
import { CreateCartUseCase } from './epplication/use-cases/create-cart/create-cart.use-case';
import { CancelOrderUseCase } from './epplication/use-cases/cancel-order/cancel-order.use-case';
import { CheckPaymentStatusUseCase } from './epplication/use-cases/check-payment-status/check-payment-status.use-case';
import { ClearCartUseCase } from './epplication/use-cases/clear-cart/clear-cart.use-case';
import { CreateOrderUseCase } from './epplication/use-cases/create-order/create-order.use-case';
import { GetAllOrdersUseCase } from './epplication/use-cases/get-all-orders/get-all-orders.use-case';
import { GetOrderDetailsUseCase } from './epplication/use-cases/get-order-detail/get-order-detail.use-case';
import { ProceedPaymentUseCase } from './epplication/use-cases/process-payment/process-payment.use-case';
import { RetrieveCartUseCase } from './epplication/use-cases/retrieve-cart/retrieve-cart.use-case';
import { ShipOrderUseCase } from './epplication/use-cases/ship-order/ship-order.use-case';
import { AddToCartUseCase } from './epplication/use-cases/add-to-cart/add-to-cart.use-case';
import { UserModule } from 'src/user/user.module';
import { MoneyMapper } from 'src/product/infrastructure/mappers/money/money.mapper';
import { CartMockService } from './core/entity/cart/mocks/cart-mock.service';
import { ProductModule } from 'src/product/product.module';
import { RemoveFromCartUseCase } from './epplication/use-cases/remove-from-cart/remove-from-cart.use-case';
import { PaymentRepository } from './infrastructure/repository/payment.repository';
import { ShipmentRepository } from './infrastructure/repository/shipment.repository';
import { CartItemMockService } from './core/entity/cart-item/mocks/cart-item-mock.service';
import { PaymentDb } from './infrastructure/entity/payment.entity';
import { ShipmentDb } from './infrastructure/entity/shipment.entity';
import { OrderDb } from './infrastructure/entity/order.entity';
import { OrderItemDb } from './infrastructure/entity/order-item.entity';
import { PaymentMapper } from './infrastructure/mappers/payment/payment.mapper';
import { ShipmentMapper } from './infrastructure/mappers/shipment/shipment.mapper';
import { OrderRepository } from './infrastructure/repository/order.repository';
import { OrderMapper } from './infrastructure/mappers/order/order.mapper';
import { OrderMockService } from './core/entity/order/mocks/order-mock.service';
import { ShipmentMockService } from './core/entity/shipment/mocks/shipment-mock.service';
import { PaymentMockService } from './core/entity/payment/mocks/payment-mock.service';
import { OrderItemMockService } from './core/entity/order-item/mocks/order-item-mock.service';

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
  providers: [
    MoneyMapper,
    CartMapper,
    CreateCartUseCase,
    CartMockService,
    CartItemMockService,
    OrderMockService,
    OrderItemMockService,
    ShipmentMockService,
    PaymentMockService,
    {
      provide: CREATE_CART_USE_CASE_TOKEN,
      useClass: CreateCartUseCase,
    },
    {
      provide: ADD_TO_CART_USE_CASE_TOKEN,
      useClass: AddToCartUseCase,
    },
    {
      provide: REMOVE_FROM_CART_USE_CASE_TOKEN,
      useClass: RemoveFromCartUseCase,
    },
    {
      provide: RETRIEVE_CART_USE_CASE_TOKEN,
      useClass: RetrieveCartUseCase,
    },
    {
      provide: CLEAR_CART_USE_CASE_TOKEN,
      useClass: ClearCartUseCase,
    },
    {
      provide: CREATE_ORDER_USE_CASE_TOKEN,
      useClass: CreateOrderUseCase,
    },
    {
      provide: GET_ORDER_DETAILS_USE_CASE_TOKEN,
      useClass: GetOrderDetailsUseCase,
    },
    {
      provide: GET_ALL_ORDERS_USE_CASE_TOKEN,
      useClass: GetAllOrdersUseCase,
    },
    {
      provide: CANCEL_ORDER_USE_CASE_TOKEN,
      useClass: CancelOrderUseCase,
    },
    {
      provide: SHIP_ORDER_USE_CASE_TOKEN,
      useClass: ShipOrderUseCase,
    },
    {
      provide: PROCEED_PAYMENT_USE_CASE_TOKEN,
      useClass: ProceedPaymentUseCase,
    },
    {
      provide: CHECK_PAYMENT_STATUS_USE_CASE_TOKEN,
      useClass: CheckPaymentStatusUseCase,
    },
    {
      provide: CART_MAPPER,
      useClass: CartMapper,
    },
    {
      provide: SHIPMENT_MAPPER,
      useClass: ShipmentMapper,
    },
    {
      provide: PAYMENT_MAPPER,
      useClass: PaymentMapper,
    },
    {
      provide: ORDER_MAPPER,
      useClass: OrderMapper,
    },
    {
      provide: CART_REPOSITORY,
      useClass: CartRepository,
    },
    {
      provide: ORDER_REPOSITORY,
      useClass: OrderRepository,
    },
    {
      provide: SHIPMENT_REPOSITORY,
      useClass: ShipmentRepository,
    },
    {
      provide: PAYMENT_REPOSITORY,
      useClass: PaymentRepository,
    },
  ],
  controllers: [CheckoutController],
})
export class CheckoutModule {}
