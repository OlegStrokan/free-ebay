import { faker } from '@faker-js/faker';
import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { UserMockService } from 'src/user/core/entity/mocks/user-mock.service';
import { GetUserByEmailUseCase } from '../get-user-by-email/get-user-by-email.use-case';
import { IGetUserByIdUseCase } from './get-user-by-id.interface';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';

describe('GetUserByEmailTest', () => {
  let getUserByIdUseCase: IGetUserByIdUseCase;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getUserByIdUseCase = module.get<IGetUserByIdUseCase>(GetUserByEmailUseCase);
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
    const retrievedUser = await getUserByIdUseCase.execute();

    expect(retrievedUser).toBeDefined();
    expect(retrievedUser.email).toBe(productName);
  });
  it("should throw error if user doesn't exist", async () => {
    await expect(getUserByIdUseCase.execute()).rejects.toThrow(
      UserNotFoundException,
    );
  });
});
