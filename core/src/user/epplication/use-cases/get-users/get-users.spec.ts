import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { UserMockService } from 'src/user/core/entity/mocks/user-mock.service';
import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing-module/clear-repos';
import { createTestingModule } from 'src/shared/testing-module/test.module';
import { IGetUsersUseCase } from './get-users.interface';
import { GetUsersUseCase } from './get-users.use-case';

describe('GetUsersUseCaseTest', () => {
  let getUsersUseCase: IGetUsersUseCase;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    getUsersUseCase = module.get<IGetUsersUseCase>(GetUsersUseCase);
    userMockService = module.get<IUserMockService>(UserMockService);
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
