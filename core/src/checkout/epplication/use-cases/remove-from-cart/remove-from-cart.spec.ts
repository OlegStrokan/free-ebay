import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { IRemoveFromCartUseCase } from './remove-from-cart.interface';
import { REMOVE_FROM_CART_USE_CASE_TOKEN } from '../../injection-tokens/use-case.token';
import { Money } from 'src/shared/types/money';
import { ICartItemMockService } from 'src/checkout/core/entity/cart-item/mocks/cart-item-mock.interface';
import { CartItemNotFoundException } from 'src/checkout/core/exceptions/cart/cart-item-not-found.exception';
import {
  CART_ITEM_MOCK_SERVICE,
  CART_MOCK_SERVICE,
} from '../../injection-tokens/mock-services.token';

describe('CreateCartUseCase', () => {
  let removeFromCartUseCase: IRemoveFromCartUseCase;
  let cartMockService: ICartMockService;
  let cartItemMockService: ICartItemMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    removeFromCartUseCase = module.get(REMOVE_FROM_CART_USE_CASE_TOKEN);
    cartMockService = module.get(CART_MOCK_SERVICE);
    cartItemMockService = module.get(CART_ITEM_MOCK_SERVICE);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully remove item to cart and recalculate total price', async () => {
    const userId = generateUlid();
    const cartId = generateUlid();
    const cartItemId = generateUlid();
    const price = Money.getDefaultMoney(100);
    const cartItem = cartItemMockService.getOne({
      cartId,
      id: cartItemId,
      quantity: 1,
      price,
    });
    await cartMockService.createOne({
      id: cartId,
      userId,
      items: [cartItem.data],
    });
    const updatedCart = await removeFromCartUseCase.execute({
      cartId,
      cartItemId: cartItem.id,
    });

    expect(updatedCart.items).toHaveLength(0);
    expect(updatedCart.totalPrice).toMatchObject(Money.getDefaultMoney(0));
  });

  it('should throw exception because cart not found', async () => {
    const cartItem = cartItemMockService.getOne();
    await expect(
      removeFromCartUseCase.execute({
        cartId: cartItem.cartId,
        cartItemId: cartItem.id,
      }),
    ).rejects.toThrow(CartNotFoundException);
  });

  it('should throw exception because cart item not found', async () => {
    const cartId = generateUlid();

    await cartMockService.createOne({ id: cartId, items: [] });

    await expect(
      removeFromCartUseCase.execute({ cartId, cartItemId: generateUlid() }),
    ).rejects.toThrow(CartItemNotFoundException);
  });
});
