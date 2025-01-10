import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { IClearCartUseCase } from './clear-cart.interface';
import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { CLEAR_CART_USE_CASE } from '../../injection-tokens/use-case.token';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { ICartItemMockService } from 'src/checkout/core/entity/cart-item/mocks/cart-item-mock.interface';
import {
  CART_ITEM_MOCK_SERVICE,
  CART_MOCK_SERVICE,
} from '../../injection-tokens/mock-services.token';

describe('ClearCartUseCaseTest', () => {
  let clearCartUseCase: IClearCartUseCase;
  let cartMockService: ICartMockService;
  let cartItemMockService: ICartItemMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();
    clearCartUseCase = module.get(CLEAR_CART_USE_CASE);
    cartMockService = module.get(CART_MOCK_SERVICE);
    cartItemMockService = module.get(CART_ITEM_MOCK_SERVICE);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });
  it('should succesfully clear cart and recalculate total price', async () => {
    const cartId = generateUlid();
    const cartItem = cartItemMockService.getOne({ cartId });
    await cartMockService.createOne({ id: cartId, items: [cartItem.data] });

    const clearedCart = await clearCartUseCase.execute(cartId);

    expect(clearedCart.items).toHaveLength(0);
    expect(clearedCart.totalPrice).toEqual(Money.getDefaultMoney());
  }),
    it('should throw exception because cart not found', async () => {
      const cartId = generateUlid();

      await expect(clearCartUseCase.execute(cartId)).rejects.toThrow(
        CartNotFoundException,
      );
    });
});
