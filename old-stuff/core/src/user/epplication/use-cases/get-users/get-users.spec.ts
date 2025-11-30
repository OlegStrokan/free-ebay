import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { TestingModule } from '@nestjs/testing';
import { IGetUsersUseCase } from './get-users.interface';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';

describe('GetUsersUseCaseTest', () => {
  let getUsersUseCase: IGetUsersUseCase;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getUsersUseCase = module.get(IGetUsersUseCase);
    userMockService = module.get(IUserMockService);
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
