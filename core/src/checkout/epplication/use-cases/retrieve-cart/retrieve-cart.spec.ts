import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { RETRIEVE_CART_USE_CASE_TOKEN } from '../../injection-tokens/use-case.token';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { CartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.service';
import { IRetrieveCartUseCase } from './retrieve-cart.interface';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { UserMockService } from 'src/user/core/entity/mocks/user-mock.service';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';

describe('RetrieveUserCartUseCase', () => {
  let retrieveCartUseCase: IRetrieveCartUseCase;
  let cartMockService: ICartMockService;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    retrieveCartUseCase = module.get(RETRIEVE_CART_USE_CASE_TOKEN);
    cartMockService = module.get(CartMockService);
    userMockService = module.get(UserMockService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully retrieve cart', async () => {
    const userId = generateUlid();
    await userMockService.createOne({ id: userId });

    await cartMockService.createOne({ userId: userId });

    const cart = await retrieveCartUseCase.execute(userId);

    expect(cart).toBeDefined();
    expect(cart.userId).toBe(userId);
  });
  it("should throw exception because user doesn't exist", async () => {
    const userId = generateUlid();
    await userMockService.createOne({ id: userId });

    await expect(retrieveCartUseCase.execute(userId)).rejects.toThrow(
      CartNotFoundException,
    );
  });
});
