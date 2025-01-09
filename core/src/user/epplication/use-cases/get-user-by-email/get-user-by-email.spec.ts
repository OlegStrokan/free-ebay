import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { faker } from '@faker-js/faker';
import { IGetUserByEmailUseCase } from './get-user-by-email.interface';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';
import { GET_USER_BY_EMAIL_USE_CASE } from '../../injection-tokens/use-case.token';
import { USER_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';

describe('GetUserByEmailTest', () => {
  let getUserByEmailUseCase: IGetUserByEmailUseCase;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getUserByEmailUseCase = module.get<IGetUserByEmailUseCase>(
      GET_USER_BY_EMAIL_USE_CASE,
    );
    userMockService = module.get<IUserMockService>(USER_MOCK_SERVICE);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully retrieve user', async () => {
    const productName = faker.internet.email();
    await userMockService.createOne({ email: productName });
    const retrievedUser = await getUserByEmailUseCase.execute();

    expect(retrievedUser).toBeDefined();
    expect(retrievedUser.email).toBe(productName);
  });
  it("should throw error if user doesn't exist", async () => {
    await expect(getUserByEmailUseCase.execute()).rejects.toThrow(
      UserNotFoundException,
    );
  });
});
