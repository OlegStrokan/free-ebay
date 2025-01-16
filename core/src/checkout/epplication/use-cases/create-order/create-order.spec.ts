import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { CREATE_ORDER_USE_CASE } from '../../injection-tokens/use-case.token';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { IOrderMockService } from 'src/checkout/core/entity/order/mocks/order-mock.interface';
import { ICreateOrderUseCase } from './create-order.interface';
import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { CartItemsNotFoundException } from 'src/checkout/core/exceptions/cart/cart-items-not-found.exception';
import { PaymentMethod } from 'src/checkout/core/entity/payment/payment';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import { CART_REPOSITORY } from '../../injection-tokens/repository.token';
import { Money } from 'src/shared/types/money';
import { ICartItemMockService } from 'src/checkout/core/entity/cart-item/mocks/cart-item-mock.interface';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import {
  CART_ITEM_MOCK_SERVICE,
  CART_MOCK_SERVICE,
  ORDER_MOCK_SERVICE,
} from '../../injection-tokens/mock-services.token';
import { USER_MOCK_SERVICE } from 'src/user/epplication/injection-tokens/mock-services.token';
import { of } from 'rxjs';
import { HttpService } from '@nestjs/axios';
import { AxiosResponse } from 'axios';

describe('CreateOrderUseCase', () => {
  let createOrderUseCase: ICreateOrderUseCase;
  let orderMockService: IOrderMockService;
  let cartMockService: ICartMockService;
  let userMockService: IUserMockService;
  let cartItemMockService: ICartItemMockService;
  let cartRepository: ICartRepository;
  let httpClient: HttpService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createOrderUseCase = module.get(CREATE_ORDER_USE_CASE);
    orderMockService = module.get(ORDER_MOCK_SERVICE);
    userMockService = module.get(USER_MOCK_SERVICE);
    cartItemMockService = module.get(CART_ITEM_MOCK_SERVICE);
    cartMockService = module.get(CART_MOCK_SERVICE);
    cartRepository = module.get(CART_REPOSITORY);
    httpClient = module.get(HttpService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully create an order from a cart', async () => {
    const cartId = generateUlid();
    const cartItemId = generateUlid();
    const userId = generateUlid();

    const price = Money.getDefaultMoney(100);
    const cartItem = cartItemMockService.getOne({
      cartId,
      id: cartItemId,
      quantity: 1,
      price,
    });
    const cart = await cartMockService.createOne({
      id: cartId,
      userId,
      items: [cartItem.data],
    });
    const dto = orderMockService.getOneToCreate({
      cartId: cart.id,
      shippingAddress: '123 Test St',
      paymentMethod: PaymentMethod.ApplePay,
    });

    await userMockService.createOne({ id: userId });

    const mockResponse: AxiosResponse = {
      data: { status: 'success', transactionId: generateUlid() },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {
        url: 'http://localhost:5012/api/Payment/ProcessPayment',
      } as any,
    };

    const sendSpy = jest
      .spyOn(httpClient, 'post')
      .mockImplementation(() => of(mockResponse));

    const order = await createOrderUseCase.execute(dto);

    const clearedCart = await cartRepository.getOneByIdIdWithRelations(cart.id);

    expect(clearedCart?.items.length).toBe(0);
    expect(clearedCart?.totalPrice).toEqual(Money.getDefaultMoney(0));
    expect(order).toBeDefined();
    expect(order.items.length).toBeGreaterThan(0);
    expect(sendSpy).toHaveBeenCalledWith(
      'http://localhost:5012/api/Payment/ProcessPayment',
      expect.objectContaining({
        orderId: expect.any(String),
        amount: expect.any(Object),
        paymentMethod: PaymentMethod.ApplePay,
      }),
    );

    sendSpy.mockRestore();
    // expect(order.data.shipment).toBeDefined();
    // expect(order.data.payment).toBeDefined();
  });

  it('should throw CartNotFoundException if cart does not exist', async () => {
    const cartId = generateUlid();

    const dto = orderMockService.getOneToCreate({
      cartId: cartId,
    });

    const userId = generateUlid();
    await userMockService.createOne({ id: userId });

    await expect(createOrderUseCase.execute(dto)).rejects.toThrow(
      CartNotFoundException,
    );
  });

  it('should throw CartItemsNotFoundException if cart is empty', async () => {
    const userId = generateUlid();
    await userMockService.createOne({ id: userId });

    const cart = await cartMockService.createOne({ items: [] });
    const dto = orderMockService.getOneToCreate({
      cartId: cart.id,
    });

    await expect(createOrderUseCase.execute(dto)).rejects.toThrow(
      CartItemsNotFoundException,
    );
  });

  //   it('should throw PaymentFailedException if payment fails', async () => {
  //     const cart = await cartMockService.createOne();
  //     const dto: CreateOrderDto = {
  //       cartId: cart.id,
  //       shippingAddress: '123 Test St',
  //       paymentMethod: PaymentMethod.CreditCard,
  //     };

  //     await expect(createOrderUseCase.execute(dto)).rejects.toThrow(
  //       PaymentFailedException,
  //     );
  //   });
});
