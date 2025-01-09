import { faker } from '@faker-js/faker';
import { TestingModule } from '@nestjs/testing';
import { ICategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.interface';
import { UpdateCategoryDto } from 'src/catalog/interface/dtos/update-category.dto';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { IUpdateCategoryUseCase } from './update-category.interface';
import { UpdateCategoryUseCase } from './update-category.use-case';
import { CategoryMockService } from 'src/catalog/core/category/entity/mocks/category-mock.service';

describe('UpdateCategoryUseCaseTest', () => {
  let updateCategoryUseCase: IUpdateCategoryUseCase;
  let categoryMockService: ICategoryMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    updateCategoryUseCase = module.get<IUpdateCategoryUseCase>(
      UpdateCategoryUseCase,
    );
    categoryMockService = module.get<ICategoryMockService>(CategoryMockService);
  });

  beforeAll(async () => {
    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should create a random Category and verify its existence', async () => {
    const categoryId = generateUlid();
    const category = await categoryMockService.createOne({
      id: categoryId,
      description: faker.commerce.department(),
    });

    const updatedCategoryData: UpdateCategoryDto = {
      description: faker.commerce.department(),
    };

    const updatedCategory = await updateCategoryUseCase.execute({
      id: category.id,
      dto: updatedCategoryData,
    });

    expect(updatedCategory).toBeDefined();
    expect(updatedCategory?.data.description).toBe(updatedCategory.description);
  }, 1000000);
});
