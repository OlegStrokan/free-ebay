import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { CartDb } from './infrastructure/entity/cart.entity';
import { CartItemDb } from './infrastructure/entity/cart-item.entity';
import { CreateCartUseCase } from './epplication/use-cases/create-cart/create-cart.spec';
import { CheckoutController } from './interface/checkout.controller';

import { CartMapper } from './infrastructure/mappers/cart/cart.mapper';
import { CartRepository } from './infrastructure/repository/cart.repository';
import { CART_REPOSITORY } from './epplication/injection-tokens/repository.token';
import { CART_MAPPER } from './epplication/injection-tokens/mapper.token';
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
} from './epplication/injection-tokens/use-case.token';

@Module({
  imports: [TypeOrmModule.forFeature([CartDb, CartItemDb])],
  exports: [],
  providers: [
    CreateCartUseCase,
    {
      provide: CREATE_CART_USE_CASE_TOKEN,
      useClass: CreateCartUseCase,
    },
    {
      provide: ADD_TO_CART_USE_CASE_TOKEN,
      useClass: AddToCartUseCase,
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
      provide: CART_REPOSITORY,
      useClass: CartRepository,
    },
  ],
  controllers: [CheckoutController],
})
export class CheckoutModule {}
