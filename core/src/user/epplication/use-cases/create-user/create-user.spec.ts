import { IUserRepository } from 'src/user/core/repository/user.repository';
import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { ICreateUserUseCase } from './create-user.interface';
import { UserAlreadyExistsException } from 'src/user/core/exceptions/user-already-exists';
import { faker } from '@faker-js/faker';
import { CREATE_USER_USE_CASE } from '../../injection-tokens/use-case.token';
import { USER_REPOSITORY } from '../../injection-tokens/repository.token';
import { USER_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';

describe('CreateUserUseCaseTest', () => {
  let createUserUseCase: ICreateUserUseCase;
  let userRepository: IUserRepository;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createUserUseCase = module.get<ICreateUserUseCase>(CREATE_USER_USE_CASE);
    userRepository = module.get<IUserRepository>(USER_REPOSITORY);
    userMockService = module.get<IUserMockService>(USER_MOCK_SERVICE);
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
    expect(user?.email).toBe(userDto.email);
  });

  it('should throw error if user already exists', async () => {
    const email = faker.internet.email();

    await userMockService.createOne({ email });

    const userToCreate = userMockService.getOneToCreate({ email });

    await expect(createUserUseCase.execute(userToCreate)).rejects.toThrow(
      UserAlreadyExistsException,
    );
  });
});
