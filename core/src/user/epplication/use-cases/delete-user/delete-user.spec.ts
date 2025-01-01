import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { UserMockService } from 'src/user/core/entity/mocks/user-mock.service';
import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing-module/clear-repos';
import { createTestingModule } from 'src/shared/testing-module/test.module';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { IDeleteUserUseCase } from './delete-user.interface';
import { DeleteUserUseCase } from './delete-user.use-case';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { UserRepository } from 'src/user/infrastructure/repository/user.repository';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';

describe('DeleteUserUseCaseTest', () => {
  let deleteUserUseCase: IDeleteUserUseCase;
  let userRepository: IUserRepository;

  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    deleteUserUseCase = module.get<IDeleteUserUseCase>(DeleteUserUseCase);
    userRepository = module.get<IUserRepository>(UserRepository);
    userMockService = module.get<IUserMockService>(UserMockService);
  });

  beforeAll(async () => {
    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully delete user', async () => {
    const userIdToDelete = generateUlid();

    await userMockService.createOne({
      id: userIdToDelete,
    });

    await deleteUserUseCase.execute(userIdToDelete);

    const user = await userRepository.findById(userIdToDelete);

    expect(user).toBe(null);
  });
  it("should throw error if user doesn't exists", async () => {
    const userIdToDelete = generateUlid();

    await expect(deleteUserUseCase.execute(userIdToDelete)).rejects.toThrow(
      UserNotFoundException,
    );
  });
});
