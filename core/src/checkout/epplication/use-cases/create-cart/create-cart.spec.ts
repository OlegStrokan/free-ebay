import { ICreateCartUseCase } from './create-cart.interface';
import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { CREATE_CART_USE_CASE_TOKEN } from '../../injection-tokens/use-case.token';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { UserMockService } from 'src/user/core/entity/mocks/user-mock.service';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';
import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { CartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.service';
import { CartAlreadyExists } from 'src/checkout/core/exceptions/cart/cart-already-exist.exception';
import { CART_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';

describe('CreateCartUseCase', () => {
  let createCartUseCase: ICreateCartUseCase;
  let userMockService: IUserMockService;
  let cartMockService: ICartMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createCartUseCase = module.get(CREATE_CART_USE_CASE_TOKEN);
    cartMockService = module.get(CART_MOCK_SERVICE);
    userMockService = module.get(UserMockService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully create cart and verify it existence', async () => {
    const userId = generateUlid();
    await userMockService.createOne({ id: userId });

    const cart = await createCartUseCase.execute({ userId });

    expect(cart).toBeDefined();
    expect(cart.userId).toBe(userId);
  });

  it('should throw exception because cart already exist', async () => {
    const userId = generateUlid();
    await userMockService.createOne({ id: userId });
    await cartMockService.createOne({ userId });

    await expect(createCartUseCase.execute({ userId })).rejects.toThrow(
      CartAlreadyExists,
    );
  });

  it("should throw exception because user doesn't exist", async () => {
    const userId = generateUlid();

    await expect(createCartUseCase.execute({ userId })).rejects.toThrow(
      UserNotFoundException,
    );
  });
});
