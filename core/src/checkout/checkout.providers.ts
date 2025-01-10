import { Provider } from '@nestjs/common';
import { CartItemMockService } from './core/entity/cart-item/mocks/cart-item-mock.service';
import { CartMockService } from './core/entity/cart/mocks/cart-mock.service';
import { OrderItemMockService } from './core/entity/order-item/mocks/order-item-mock.service';
import { OrderMockService } from './core/entity/order/mocks/order-mock.service';
import { PaymentMockService } from './core/entity/payment/mocks/payment-mock.service';
import { ShipmentMockService } from './core/entity/shipment/mocks/shipment-mock.service';
import {
  CART_MAPPER,
  SHIPMENT_MAPPER,
  PAYMENT_MAPPER,
  ORDER_MAPPER,
} from './epplication/injection-tokens/mapper.token';
import {
  CART_MOCK_SERVICE,
  CART_ITEM_MOCK_SERVICE,
  ORDER_MOCK_SERVICE,
  ORDER_ITEM_MOCK_SERVICE,
  SHIPMENT_MOCK_SERVICE,
  PAYMENT_MOCK_SERVICE,
} from './epplication/injection-tokens/mock-services.token';
import {
  CART_REPOSITORY,
  ORDER_REPOSITORY,
  SHIPMENT_REPOSITORY,
  PAYMENT_REPOSITORY,
} from './epplication/injection-tokens/repository.token';
import {
  CREATE_CART_USE_CASE,
  ADD_TO_CART_USE_CASE,
  REMOVE_FROM_CART_USE_CASE,
  RETRIEVE_CART_USE_CASE,
  CLEAR_CART_USE_CASE,
  CREATE_ORDER_USE_CASE,
  GET_ORDER_DETAIL_USE_CASE,
  GET_ALL_USER_ORDERS_USE_CASE,
  CANCEL_ORDER_USE_CASE,
  SHIP_ORDER_USE_CASE,
  PROCEED_PAYMENT_USE_CASE,
  CHECK_PAYMENT_STATUS_USE_CASE,
} from './epplication/injection-tokens/use-case.token';
import { AddToCartUseCase } from './epplication/use-cases/add-to-cart/add-to-cart.use-case';
import { CancelOrderUseCase } from './epplication/use-cases/cancel-order/cancel-order.use-case';
import { CheckPaymentStatusUseCase } from './epplication/use-cases/check-payment-status/check-payment-status.use-case';
import { ClearCartUseCase } from './epplication/use-cases/clear-cart/clear-cart.use-case';
import { CreateCartUseCase } from './epplication/use-cases/create-cart/create-cart.use-case';
import { CreateOrderUseCase } from './epplication/use-cases/create-order/create-order.use-case';
import { GetAllUserOrdersUseCase } from './epplication/use-cases/get-all-user-orders/get-all-user-orders.use-case';
import { GetOrderDetailsUseCase } from './epplication/use-cases/get-order-detail/get-order-detail.use-case';
import { ProceedPaymentUseCase } from './epplication/use-cases/process-payment/process-payment.use-case';
import { RemoveFromCartUseCase } from './epplication/use-cases/remove-from-cart/remove-from-cart.use-case';
import { RetrieveCartUseCase } from './epplication/use-cases/retrieve-cart/retrieve-cart.use-case';
import { ShipOrderUseCase } from './epplication/use-cases/ship-order/ship-order.use-case';
import { CartMapper } from './infrastructure/mappers/cart/cart.mapper';
import { OrderMapper } from './infrastructure/mappers/order/order.mapper';
import { PaymentMapper } from './infrastructure/mappers/payment/payment.mapper';
import { ShipmentMapper } from './infrastructure/mappers/shipment/shipment.mapper';
import { CartRepository } from './infrastructure/repository/cart.repository';
import { OrderRepository } from './infrastructure/repository/order.repository';
import { PaymentRepository } from './infrastructure/repository/payment.repository';
import { ShipmentRepository } from './infrastructure/repository/shipment.repository';

export const checkoutProviders: Provider[] = [
  {
    provide: CART_MOCK_SERVICE,
    useClass: CartMockService,
  },
  {
    provide: CART_ITEM_MOCK_SERVICE,
    useClass: CartItemMockService,
  },
  {
    provide: ORDER_MOCK_SERVICE,
    useClass: OrderMockService,
  },
  {
    provide: ORDER_ITEM_MOCK_SERVICE,
    useClass: OrderItemMockService,
  },
  {
    provide: SHIPMENT_MOCK_SERVICE,
    useClass: ShipmentMockService,
  },
  {
    provide: PAYMENT_MOCK_SERVICE,
    useClass: PaymentMockService,
  },
  {
    provide: CREATE_CART_USE_CASE,
    useClass: CreateCartUseCase,
  },
  {
    provide: ADD_TO_CART_USE_CASE,
    useClass: AddToCartUseCase,
  },
  {
    provide: REMOVE_FROM_CART_USE_CASE,
    useClass: RemoveFromCartUseCase,
  },
  {
    provide: RETRIEVE_CART_USE_CASE,
    useClass: RetrieveCartUseCase,
  },
  {
    provide: CLEAR_CART_USE_CASE,
    useClass: ClearCartUseCase,
  },
  {
    provide: CREATE_ORDER_USE_CASE,
    useClass: CreateOrderUseCase,
  },
  {
    provide: GET_ORDER_DETAIL_USE_CASE,
    useClass: GetOrderDetailsUseCase,
  },
  {
    provide: GET_ALL_USER_ORDERS_USE_CASE,
    useClass: GetAllUserOrdersUseCase,
  },
  {
    provide: CANCEL_ORDER_USE_CASE,
    useClass: CancelOrderUseCase,
  },
  {
    provide: SHIP_ORDER_USE_CASE,
    useClass: ShipOrderUseCase,
  },
  {
    provide: PROCEED_PAYMENT_USE_CASE,
    useClass: ProceedPaymentUseCase,
  },
  {
    provide: CHECK_PAYMENT_STATUS_USE_CASE,
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
];
