import { IUserMockService } from 'src/user/core/entity/mocks/user-mock.interface';
import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { IDeleteUserUseCase } from './delete-user.interface';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { UserNotFoundException } from 'src/user/core/exceptions/user-not-found.exception';

describe('DeleteUserUseCaseTest', () => {
  let deleteUserUseCase: IDeleteUserUseCase;
  let userRepository: IUserRepository;

  let userMockService: IUserMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    deleteUserUseCase = module.get(IDeleteUserUseCase);
    userRepository = module.get(IUserRepository);
    userMockService = module.get(IUserMockService);
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
