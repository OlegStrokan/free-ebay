import { ICreateCartUseCase } from './create-cart.interface';
import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';
import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { CartAlreadyExists } from 'src/checkout/core/exceptions/cart/cart-already-exist.exception';

describe('CreateCartUseCase', () => {
  let createCartUseCase: ICreateCartUseCase;
  let userMockService: IUserMockService;
  let cartMockService: ICartMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createCartUseCase = module.get(ICreateCartUseCase);
    cartMockService = module.get(ICartMockService);
    userMockService = module.get(IUserMockService);

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
