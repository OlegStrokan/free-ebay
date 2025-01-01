import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing-module/clear-repos';
import { createTestingModule } from 'src/shared/testing-module/test.module';
import { faker } from '@faker-js/faker';
import { IGetUserByEmailUseCase } from './get-user-by-email.interface';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { UserMockService } from 'src/user/core/entity/mocks/user-mock.service';
import { GetUserByEmailUseCase } from './get-user-by-email.use-case';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';

describe('GetUserByEmailTest', () => {
  let getUserByEmailUseCase: IGetUserByEmailUseCase;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getUserByEmailUseCase = module.get<IGetUserByEmailUseCase>(
      GetUserByEmailUseCase,
    );
    userMockService = module.get<IUserMockService>(UserMockService);
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
