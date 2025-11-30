import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { faker } from '@faker-js/faker';
import { IGetUserByEmailUseCase } from './get-user-by-email.interface';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';

describe('GetUserByEmailTest', () => {
  let getUserByEmailUseCase: IGetUserByEmailUseCase;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getUserByEmailUseCase = module.get(IGetUserByEmailUseCase);
    userMockService = module.get(IUserMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully retrieve user', async () => {
    const userEmail = faker.internet.email();
    await userMockService.createOne({ email: userEmail });
    const retrievedUser = await getUserByEmailUseCase.execute(userEmail);

    expect(retrievedUser).toBeDefined();
    expect(retrievedUser.email).toBe(userEmail);
  });
  it("should throw error if user doesn't exist", async () => {
    await expect(
      getUserByEmailUseCase.execute('non_existing_email'),
    ).rejects.toThrow(UserNotFoundException);
  });
});
