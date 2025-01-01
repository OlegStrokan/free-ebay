import { TestingModule } from '@nestjs/testing';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { ICreateCategoryUseCase } from './create-category.interface';
import { ICategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.interface';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { CreateCategoryUseCase } from './create-category.use-case';
import { CategoryRepository } from 'src/catalog/infrastructure/repository/category.repository';
import { CategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.service';
import { faker } from '@faker-js/faker';
import { CategoryAlreadyExistsException } from 'src/catalog/core/category/entity/exceptions/category-already-exists.exception';

describe('CreateCategoryUseCaseTest', () => {
  let createCategoryUseCase: ICreateCategoryUseCase;
  let categoryRepository: ICategoryRepository;
  let categoryMockService: ICategoryMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    createCategoryUseCase = module.get<ICreateCategoryUseCase>(
      CreateCategoryUseCase,
    );
    categoryRepository = module.get<ICategoryRepository>(CategoryRepository);
    categoryMockService = module.get<ICategoryMockService>(CategoryMockService);
  });

  beforeEach(async () => {
    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should create a random category and verify its existence', async () => {
    const categoryName = faker.commerce.department();

    const categoryDto = categoryMockService.getOneToCreateWithoutParentId({
      name: categoryName,
    });

    await createCategoryUseCase.execute(categoryDto);
    const category = await categoryRepository.findByName(categoryName);

    expect(category).toBeDefined();
    expect(category?.name).toBe(categoryName);
  });

  it('should throw error if category already exists', async () => {
    const categoryName = faker.commerce.department();

    const categoryDto = categoryMockService.getOneToCreateWithoutParentId({
      name: categoryName,
    });
    await categoryMockService.createOne({ name: categoryDto.name });

    await expect(createCategoryUseCase.execute(categoryDto)).rejects.toThrow(
      CategoryAlreadyExistsException,
    );
  });
});
