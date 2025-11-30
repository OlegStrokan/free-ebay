import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { IGetUserByIdUseCase } from './get-user-by-id.interface';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';
import { generateUlid } from 'src/shared/types/generate-ulid';

describe('GetUserByEmailTest', () => {
  let getUserByIdUseCase: IGetUserByIdUseCase;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getUserByIdUseCase = module.get(IGetUserByIdUseCase);
    userMockService = module.get(IUserMockService);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  it('should succesfully retrieve user', async () => {
    const userId = generateUlid();
    await userMockService.createOne({ id: userId });
    const retrievedUser = await getUserByIdUseCase.execute(userId);

    expect(retrievedUser).toBeDefined();
    expect(retrievedUser.id).toBe(userId);
  });
  it("should throw error if user doesn't exist", async () => {
    await expect(getUserByIdUseCase.execute('non_existing_id')).rejects.toThrow(
      UserNotFoundException,
    );
  });
});
