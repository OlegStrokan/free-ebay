import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { IOrderMockService } from 'src/checkout/core/entity/order/mocks/order-mock.interface';
import { ICreateOrderUseCase } from './create-order.interface';
import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { CartItemsNotFoundException } from 'src/checkout/core/exceptions/cart/cart-items-not-found.exception';
import { PaymentMethod } from 'src/checkout/core/entity/payment/payment';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import { Money } from 'src/shared/types/money';
import { ICartItemMockService } from 'src/checkout/core/entity/cart-item/mocks/cart-item-mock.interface';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { PaymentGrpcService } from 'src/shared/grpc/payment-grpc.service';
import { PaymentFailedException } from 'src/checkout/core/exceptions/payment/payment-failed.exception';

describe('CreateOrderUseCase', () => {
  let createOrderUseCase: ICreateOrderUseCase;
  let orderMockService: IOrderMockService;
  let cartMockService: ICartMockService;
  let userMockService: IUserMockService;
  let cartItemMockService: ICartItemMockService;
  let cartRepository: ICartRepository;
  let paymentGrpcService: PaymentGrpcService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createOrderUseCase = module.get(ICreateOrderUseCase);
    orderMockService = module.get(IOrderMockService);
    userMockService = module.get(IUserMockService);
    cartItemMockService = module.get(ICartItemMockService);
    cartMockService = module.get(ICartMockService);
    cartRepository = module.get(ICartRepository);
    paymentGrpcService = module.get(PaymentGrpcService);

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

    // Mock the gRPC payment service response
    const mockResponse = {
      status: 200,
      data: {
        paymentStatus: 'success',
        transactionId: generateUlid(),
        clientSecret: '',
        error: undefined,
      },
    };
    const grpcSpy = jest
      .spyOn(paymentGrpcService, 'processPayment')
      .mockResolvedValue(mockResponse);

    const order = await createOrderUseCase.execute(dto);

    const clearedCart = await cartRepository.getOneByIdIdWithRelations(cart.id);

    expect(clearedCart?.items.length).toBe(0);
    expect(clearedCart?.totalPrice).toEqual(Money.getDefaultMoney(0));
    expect(order).toBeDefined();
    expect(order.items.length).toBeGreaterThan(0);
    expect(grpcSpy).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(String),
      expect.any(Money),
      PaymentMethod.ApplePay,
    );

    grpcSpy.mockRestore();
    expect(order.data.shipment).toBeDefined();
    expect(order.data.payment).toBeDefined();
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

  it('should throw PaymentFailedException if payment fails', async () => {
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
      paymentMethod: PaymentMethod.Card,
    });

    await userMockService.createOne({ id: userId });

    await expect(createOrderUseCase.execute(dto)).rejects.toThrow(
      PaymentFailedException,
    );
  });
});
