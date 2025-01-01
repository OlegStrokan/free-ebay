import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { UserMockService } from 'src/user/core/entity/mocks/user-mock.service';
import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing-module/clear-repos';
import { createTestingModule } from 'src/shared/testing-module/test.module';
import { IUpdateUserUseCase } from './update-user.interface';
import { faker } from '@faker-js/faker';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { UpdateUserDto } from 'src/user/interface/dtos/update-user.dto';
import { UpdateUserRequest } from './update-user.use-case';
import { UpdateUserUseCase } from './update-user.use-case';

describe('UpdateUserUseCaseTest', () => {
  let updateUserUseCase: IUpdateUserUseCase;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    updateUserUseCase = module.get<IUpdateUserUseCase>(UpdateUserUseCase);
    userMockService = module.get<IUserMockService>(UserMockService);
  });

  beforeAll(async () => {
    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should create a random user and verify its existence', async () => {
    const userData: UpdateUserRequest = {
      id: generateUlid(),
      dto: {
        email: faker.internet.email(),
      },
    };

    await userMockService.createOne({
      id: userData.id,
      email: userData.dto.email,
    });

    const updatedUserData: UpdateUserDto = { email: faker.internet.email() };

    const updatedUser = await updateUserUseCase.execute({
      id: userData.id,
      dto: updatedUserData,
    });

    expect(updatedUser).toBeDefined();
    expect(updatedUser?.email).toBe(updatedUser.email);
  });
});