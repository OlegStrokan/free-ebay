import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { IRetrieveCartUseCase } from './retrieve-cart.interface';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';

describe('RetrieveUserCartUseCase', () => {
  let retrieveCartUseCase: IRetrieveCartUseCase;
  let cartMockService: ICartMockService;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    retrieveCartUseCase = module.get(IRetrieveCartUseCase);
    cartMockService = module.get(ICartMockService);
    userMockService = module.get(IUserMockService);

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
