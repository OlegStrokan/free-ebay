import { Category, CategoryData } from './category';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { ProductMockService } from 'src/product/core/product/entity/mocks/product-mock.service';

describe('Category', () => {
  let productMockService: IProductMockService;
  let module: TestingModule;
  let categoryData: CategoryData;
  let category: Category;

  beforeAll(async () => {
    module = await createTestingModule();

    productMockService = module.get(ProductMockService);
  }),
    beforeEach(() => {
      categoryData = {
        id: generateUlid(),
        name: 'Electronics',
        description: 'Devices and gadgets',
        products: [],
        children: [],
      };
      category = new Category(categoryData);
    });

  afterAll(async () => {
    await module.close();
  });

  test('should create a category successfully', () => {
    expect(category).toBeInstanceOf(Category);
    expect(category.name).toBe('Electronics');
    expect(category.description).toBe('Devices and gadgets');
    expect(category.children).toHaveLength(0);
    expect(category.products).toHaveLength(0);
  });

  test('should add a child category successfully', () => {
    const childCategory = Category.create({
      name: 'Mobile Phones',
      description: 'Smartphones and accessories',
      products: [],
    });
    category.addChild(childCategory);
    expect(category.children).toHaveLength(1);
    expect(category.children[0].name).toBe('Mobile Phones');
  });

  test('should add a product successfully', () => {
    const product = productMockService.getOne({
      id: generateUlid(),
      name: 'Smartphone',
      price: Money.getDefaultMoney(699),
    });
    category.addProduct(product);
    expect(category.products).toHaveLength(1);
    expect(category.products[0].name).toBe('Smartphone');
  });

  test('should change category name successfully', () => {
    category.changeName('Home Appliances');
    expect(category.name).toBe('Home Appliances');
  });

  test('should change category description successfully', () => {
    category.changeDescription('Appliances for home use');
    expect(category.description).toBe('Appliances for home use');
  });

  test('should retain original category data after changes', () => {
    const originalName = category.name;
    const originalDescription = category.description;

    category.changeName('Computers');
    category.changeDescription('All about computers');

    expect(originalName).toBe('Electronics');
    expect(originalDescription).toBe('Devices and gadgets');
  });
});
