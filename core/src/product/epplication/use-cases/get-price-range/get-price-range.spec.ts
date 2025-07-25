import { IGetPriceRangeUseCase } from './get-price-range.interface';
import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { GetPriceRangeDto } from 'src/product/interface/dtos/get-price-range.dto';

describe('GetPriceRangeUseCaseTest', () => {
  let getPriceRangeUseCase: IGetPriceRangeUseCase;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();
    getPriceRangeUseCase = module.get(IGetPriceRangeUseCase);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should analyze price range for a product', async () => {
    const dto: GetPriceRangeDto = {
      title: 'iPhone 15 Pro Max',
      description: 'Latest iPhone with advanced camera system and A17 Pro chip',
      price: 119900, // $1199.00 in cents
    };

    const result = await getPriceRangeUseCase.execute(dto);

    expect(result).toBeDefined();
    expect(typeof result).toBe('string');
    expect(result).toContain('Price Range:');
    expect(result).toContain('Conclusion:');
    expect(result).toContain('Analysis:');
  });

  it('should handle different product types', async () => {
    const dto: GetPriceRangeDto = {
      title: 'Samsung Galaxy S24 Ultra',
      description:
        'Premium Android smartphone with S Pen and advanced AI features',
      price: 129900, // $1299.00 in cents
    };

    const result = await getPriceRangeUseCase.execute(dto);

    expect(result).toBeDefined();
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });
});
