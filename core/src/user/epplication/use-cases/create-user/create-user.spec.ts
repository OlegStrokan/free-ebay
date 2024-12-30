import { IUserRepository } from 'src/user/core/repository/user.repository';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { UserMockService } from 'src/user/core/entity/mocks/user-mock.service';
import { UserRepository } from 'src/user/infrastructure/repository/user.repository';
import { CreateUserUseCase } from './create-user.use-case';
import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing-module/clear-repos';
import { createTestingModule } from 'src/shared/testing-module/test.module';
import { ICreateUserUseCase } from './create-user.interface';

describe('CreateUserUseCaseTest', () => {
  let createUserUseCase: ICreateUserUseCase;
  let userRepository: IUserRepository;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createUserUseCase = module.get<ICreateUserUseCase>(CreateUserUseCase);
    userRepository = module.get<IUserRepository>(UserRepository);
    userMockService = module.get<IUserMockService>(UserMockService);
  });

  beforeAll(async () => {
    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should create a random user and verify its existence', async () => {
    const userDto = userMockService.getOneToCreate();

    const response = await createUserUseCase.execute(userDto);

    expect(response).toBeDefined();
    // TODO - update response type
    expect(response).toBe('OK');
    const user = await userRepository.findByEmail(userDto.email);

    expect(user).toBeDefined();
    expect(user.email).toBe(userDto.email);
  });
});
