import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { IClearCartUseCase } from './clear-cart.interface';
import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { ICartItemMockService } from 'src/checkout/core/entity/cart-item/mocks/cart-item-mock.interface';

describe('ClearCartUseCaseTest', () => {
  let clearCartUseCase: IClearCartUseCase;
  let cartMockService: ICartMockService;
  let cartItemMockService: ICartItemMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();
    clearCartUseCase = module.get(IClearCartUseCase);
    cartMockService = module.get(ICartMockService);
    cartItemMockService = module.get(ICartItemMockService);

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
