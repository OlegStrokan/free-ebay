import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { ICartMapper } from './cart.mapper.interface';
import { CartData } from 'src/checkout/core/entity/cart/cart';
import { Cart } from 'src/checkout/core/entity/cart/cart';
import { CartDb } from '../../entity/cart.entity';
import { Money } from 'src/shared/types/money';
import { CartItemDb } from '../../entity/cart-item.entity';
import { CART_MAPPER } from 'src/checkout/epplication/injection-tokens/mapper.token';
import { generateUlid } from 'src/shared/types/generate-ulid';

const validateCartDataStructure = (cartData: CartData | undefined) => {
  if (!cartData) throw new Error('Cart not found test error');

  expect(cartData).toEqual({
    id: expect.any(String),
    userId: expect.any(String),
    items: expect.arrayContaining([
      expect.objectContaining({
        id: expect.any(String),
        productId: expect.any(String),
        quantity: expect.any(Number),
        price: expect.objectContaining({
          amount: expect.any(Number),
          currency: expect.any(String),
          fraction: expect.any(Number),
        }),
        createdAt: expect.any(Date),
        updatedAt: expect.any(Date),
      }),
    ]),
    totalPrice: expect.objectContaining({
      amount: expect.any(Number),
      currency: expect.any(String),
      fraction: expect.any(Number),
    }),
    createdAt: expect.any(Date),
    updatedAt: expect.any(Date),
  });
};

describe('CartMapperTest', () => {
  let module: TestingModule;
  let cartMapper: ICartMapper<CartData, Cart, CartDb>;

  beforeAll(async () => {
    module = await createTestingModule();

    cartMapper = module.get<ICartMapper<CartData, Cart, CartDb>>(CART_MAPPER);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully transform domain cart to client (dto) cart', async () => {
    const domainCart = new Cart({
      id: 'cart123',
      userId: 'user123',
      items: [
        {
          id: generateUlid(),
          productId: generateUlid(),
          quantity: 2,
          price: new Money(200, 'USD', 100),
          createdAt: new Date(),
          updatedAt: new Date(),
          cartId: generateUlid(),
        },
      ],
      totalPrice: new Money(400, 'USD', 100),
      createdAt: new Date(),
      updatedAt: new Date(),
    });

    const dtoCart = cartMapper.toClient(domainCart);
    validateCartDataStructure(dtoCart);
  });

  it('should successfully map database cart to domain cart', async () => {
    const cartDb = new CartDb();
    cartDb.id = 'cart123';
    cartDb.userId = 'user123';
    cartDb.items = [
      {
        id: generateUlid(),
        productId: generateUlid(),
        createdAt: new Date(),
        updatedAt: new Date(),
        quantity: 2,
        price: JSON.stringify({
          amount: 200,
          currency: 'USD',
          fraction: 100,
        }),
      } as CartItemDb,
    ];
    cartDb.totalPrice = JSON.stringify({
      amount: 400,
      currency: 'USD',
      fraction: 100,
    });
    cartDb.createdAt = new Date();
    cartDb.updatedAt = new Date();

    const domainCart = cartMapper.toDomain(cartDb);
    validateCartDataStructure(domainCart.data);
  });
});
