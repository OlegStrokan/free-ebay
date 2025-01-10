import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { ICreateCategoryUseCase } from './create-category.interface';
import { ICategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.interface';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { faker } from '@faker-js/faker';
import { CategoryAlreadyExistsException } from 'src/catalog/core/category/entity/exceptions/category-already-exists.exception';
import { CATEGORY_MOCK_SERVICE } from '../../injection-tokens/mock-services.token';
import { CREATE_CATEGORY_USE_CASE } from '../../injection-tokens/use-case.token';
import { CATEGORY_REPOSITORY } from '../../injection-tokens/repository.token';

describe('CreateCategoryUseCaseTest', () => {
  let createCategoryUseCase: ICreateCategoryUseCase;
  let categoryRepository: ICategoryRepository;
  let categoryMockService: ICategoryMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createCategoryUseCase = module.get<ICreateCategoryUseCase>(
      CREATE_CATEGORY_USE_CASE,
    );
    categoryRepository = module.get<ICategoryRepository>(CATEGORY_REPOSITORY);
    categoryMockService = module.get<ICategoryMockService>(
      CATEGORY_MOCK_SERVICE,
    );
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should create a random category and verify its existence', async () => {
    const categoryName = faker.commerce.department();

    const categoryDto = categoryMockService.getOneToCreate({
      name: categoryName,
    });
    await createCategoryUseCase.execute(categoryDto);
    const category = await categoryRepository.findByName(categoryName);

    expect(category).toBeDefined();
    expect(category?.name).toBe(categoryName);
  });

  it('should throw error if category already exists', async () => {
    const categoryName = faker.commerce.department();

    const categoryDto = categoryMockService.getOne({
      name: categoryName,
    });
    await categoryMockService.createOne({ name: categoryDto.name });

    await expect(createCategoryUseCase.execute(categoryDto)).rejects.toThrow(
      CategoryAlreadyExistsException,
    );
  });
});
