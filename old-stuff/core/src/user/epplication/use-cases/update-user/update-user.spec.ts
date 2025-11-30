import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { IUpdateUserUseCase, UpdateUserRequest } from './update-user.interface';
import { faker } from '@faker-js/faker';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { UpdateUserDto } from 'src/user/interface/dtos/update-user.dto';

describe('UpdateUserUseCaseTest', () => {
  let updateUserUseCase: IUpdateUserUseCase;
  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    updateUserUseCase = module.get(IUpdateUserUseCase);
    userMockService = module.get(IUserMockService);
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
