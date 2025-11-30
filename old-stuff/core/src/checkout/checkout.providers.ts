import { Provider } from '@nestjs/common';
import { CartItemMockService } from './core/entity/cart-item/mocks/cart-item-mock.service';
import { CartMockService } from './core/entity/cart/mocks/cart-mock.service';
import { OrderItemMockService } from './core/entity/order-item/mocks/order-item-mock.service';
import { OrderMockService } from './core/entity/order/mocks/order-mock.service';
import { PaymentMockService } from './core/entity/payment/mocks/payment-mock.service';
import { ShipmentMockService } from './core/entity/shipment/mocks/shipment-mock.service';
import { AddToCartUseCase } from './epplication/use-cases/add-to-cart/add-to-cart.use-case';
import { CancelOrderUseCase } from './epplication/use-cases/cancel-order/cancel-order.use-case';
import { CheckPaymentStatusUseCase } from './epplication/use-cases/check-payment-status/check-payment-status.use-case';
import { ClearCartUseCase } from './epplication/use-cases/clear-cart/clear-cart.use-case';
import { CreateCartUseCase } from './epplication/use-cases/create-cart/create-cart.use-case';
import { CreateOrderUseCase } from './epplication/use-cases/create-order/create-order.use-case';
import { GetAllUserOrdersUseCase } from './epplication/use-cases/get-all-user-orders/get-all-user-orders.use-case';
import { GetOrderDetailsUseCase } from './epplication/use-cases/get-order-detail/get-order-detail.use-case';
import { RemoveFromCartUseCase } from './epplication/use-cases/remove-from-cart/remove-from-cart.use-case';
import { RetrieveCartUseCase } from './epplication/use-cases/retrieve-cart/retrieve-cart.use-case';
import { ShipOrderUseCase } from './epplication/use-cases/ship-order/ship-order.use-case';
import { CartRepository } from './infrastructure/repository/cart.repository';
import { OrderRepository } from './infrastructure/repository/order.repository';
import { PaymentRepository } from './infrastructure/repository/payment.repository';
import { ShipmentRepository } from './infrastructure/repository/shipment.repository';
import { CartMapper } from './infrastructure/mappers/cart/cart.mapper';
import { OrderMapper } from './infrastructure/mappers/order/order.mapper';
import { PaymentMapper } from './infrastructure/mappers/payment/payment.mapper';
import { ShipmentMapper } from './infrastructure/mappers/shipment/shipment.mapper';
import { ICartRepository } from './core/repository/cart.repository';
import { IOrderRepository } from './core/repository/order.repository';
import { IPaymentRepository } from './core/repository/payment.repository';
import { IShipmentRepository } from './core/repository/shipment.repository';
import { ICartMapper } from './infrastructure/mappers/cart/cart.mapper.interface';
import { IOrderMapper } from './infrastructure/mappers/order/order.mapper.interface';
import { IShipmentMapper } from './infrastructure/mappers/shipment/shipment.mapper.interface';
import { IPaymentMapper } from './infrastructure/mappers/payment/payment.mapper.inteface';
import { ICreateCartUseCase } from './epplication/use-cases/create-cart/create-cart.interface';
import { IAddToCartUseCase } from './epplication/use-cases/add-to-cart/add-to-cart.interface';
import { ICancelOrderUseCase } from './epplication/use-cases/cancel-order/cancel-order.interface';
import { ICheckPaymentStatusUseCase } from './epplication/use-cases/check-payment-status/check-payment-status.interface';
import { IClearCartUseCase } from './epplication/use-cases/clear-cart/clear-cart.interface';
import { ICreateOrderUseCase } from './epplication/use-cases/create-order/create-order.interface';
import { IGetAllUserOrdersUseCase } from './epplication/use-cases/get-all-user-orders/get-all-user-orders.interface';
import { IGetOrderDetailsUseCase } from './epplication/use-cases/get-order-detail/get-order-detail.interface';
import { IRemoveFromCartUseCase } from './epplication/use-cases/remove-from-cart/remove-from-cart.interface';
import { IRetrieveCartUseCase } from './epplication/use-cases/retrieve-cart/retrieve-cart.interface';
import { IShipOrderUseCase } from './epplication/use-cases/ship-order/ship-order.interface';
import { ICartItemMockService } from './core/entity/cart-item/mocks/cart-item-mock.interface';
import { ICartMockService } from './core/entity/cart/mocks/cart-mock.interface';
import { IOrderItemMockService } from './core/entity/order-item/mocks/order-item-mock.interface';
import { IOrderMockService } from './core/entity/order/mocks/order-mock.interface';
import { IPaymentMockService } from './core/entity/payment/mocks/payment-mock.inteface';
import { IShipmentMockService } from './core/entity/shipment/mocks/shipment-mock.interface';
import { IUpdatePaymentStatusUseCase } from './epplication/use-cases/update-payment-status/update-payment-status.interface';
import { UpdatePaymentStatusUseCase } from './epplication/use-cases/update-payment-status/update-payment-status.use-case';
import { IMoneyMapper } from 'src/product/infrastructure/mappers/money/money.mapper.interface';
import { MoneyMapper } from 'src/product/infrastructure/mappers/money/money.mapper';

export const checkoutProviders: Provider[] = [
  {
    provide: ICartRepository,
    useClass: CartRepository,
  },
  {
    provide: IOrderRepository,
    useClass: OrderRepository,
  },
  {
    provide: IShipmentRepository,
    useClass: ShipmentRepository,
  },
  {
    provide: IPaymentRepository,
    useClass: PaymentRepository,
  },
  {
    provide: ICartMapper,
    useClass: CartMapper,
  },
  {
    provide: IMoneyMapper,
    useClass: MoneyMapper,
  },
  {
    provide: IOrderMapper,
    useClass: OrderMapper,
  },
  {
    provide: IShipmentMapper,
    useClass: ShipmentMapper,
  },
  {
    provide: IPaymentMapper,
    useClass: PaymentMapper,
  },
  {
    provide: ICartMockService,
    useClass: CartMockService,
  },
  {
    provide: ICartItemMockService,
    useClass: CartItemMockService,
  },
  {
    provide: IOrderMockService,
    useClass: OrderMockService,
  },
  {
    provide: IOrderItemMockService,
    useClass: OrderItemMockService,
  },
  {
    provide: IShipmentMockService,
    useClass: ShipmentMockService,
  },
  {
    provide: IPaymentMockService,
    useClass: PaymentMockService,
  },
  {
    provide: ICreateCartUseCase,
    useClass: CreateCartUseCase,
  },
  {
    provide: IAddToCartUseCase,
    useClass: AddToCartUseCase,
  },
  {
    provide: IRemoveFromCartUseCase,
    useClass: RemoveFromCartUseCase,
  },
  {
    provide: IRetrieveCartUseCase,
    useClass: RetrieveCartUseCase,
  },
  {
    provide: IClearCartUseCase,
    useClass: ClearCartUseCase,
  },
  {
    provide: ICreateOrderUseCase,
    useClass: CreateOrderUseCase,
  },
  {
    provide: IGetOrderDetailsUseCase,
    useClass: GetOrderDetailsUseCase,
  },
  {
    provide: IGetAllUserOrdersUseCase,
    useClass: GetAllUserOrdersUseCase,
  },
  {
    provide: ICancelOrderUseCase,
    useClass: CancelOrderUseCase,
  },
  {
    provide: IShipOrderUseCase,
    useClass: ShipOrderUseCase,
  },
  {
    provide: ICheckPaymentStatusUseCase,
    useClass: CheckPaymentStatusUseCase,
  },
  {
    provide: IUpdatePaymentStatusUseCase,
    useClass: UpdatePaymentStatusUseCase,
  },
];
