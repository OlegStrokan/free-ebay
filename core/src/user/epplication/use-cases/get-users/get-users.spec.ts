import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { TestingModule } from '@nestjs/testing';
import { IGetUsersUseCase } from './get-users.interface';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { GET_USERS_USE_CASE } from '../../injection-tokens/use-case.token';
import { USER_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';

describe('GetUsersUseCaseTest', () => {
  let getUsersUseCase: IGetUsersUseCase;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getUsersUseCase = module.get<IGetUsersUseCase>(GET_USERS_USE_CASE);
    userMockService = module.get<IUserMockService>(USER_MOCK_SERVICE);
  });

  beforeAll(async () => {
    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should succesfully retrieve all users', async () => {
    await Promise.all([
      userMockService.createOne(),
      userMockService.createOne(),
    ]);

    const users = await getUsersUseCase.execute();

    expect(users).toBeDefined();
    expect(users.length).toBe(2);
  });
});
