import { Product } from './product';
import { ProductData } from './product.interface';
import { ProductStatus } from './product-status';
import { Money } from 'src/shared/types/money';
import { InvalidProductStatusException } from '../exceptions/invalid-product-status.exception';
import { RestockQuantityException } from '../exceptions/restock-quantity.exception';

describe('Product', () => {
  let productData: ProductData;
  let product: Product;

  beforeEach(() => {
    productData = {
      id: '1',
      name: 'Test Product',
      description: 'A product for testing',
      sku: 'test-product-data',
      price: new Money(100, 'USD', 2),
      status: ProductStatus.Available,
      createdAt: new Date(),
      updatedAt: new Date(),
      stock: 0,
    };
    product = new Product(productData);
  });

  test('should create a product successfully', () => {
    const newProduct = Product.create({
      name: 'New Product',
      description: 'A new product',
      price: new Money(150, 'USD', 2),
      sku: 'new-product-1',
    });
    expect(newProduct).toBeInstanceOf(Product);
    expect(newProduct.name).toBe('New Product');
    expect(newProduct.price.getAmount()).toBe(150);
    expect(newProduct.currentStatus).toBe(ProductStatus.Available);
    expect(newProduct.stock).toBe(0);
  });

  test('should update price successfully', () => {
    const updatedProduct = product.updatePrice(new Money(120, 'USD', 2));
    expect(updatedProduct.price.getAmount()).toBe(120);
  });

  test('should mark product as out of stock', () => {
    const updatedProduct = product.markAsOutOfStock();
    expect(updatedProduct.currentStatus).toBe(ProductStatus.OutOfStock);
  });

  test('should mark product as available', () => {
    product = product.markAsOutOfStock();
    const updatedProduct = product.markAsAvailable();
    expect(updatedProduct.currentStatus).toBe(ProductStatus.Available);
  });

  test('should apply discount successfully', () => {
    const updatedProduct = product.applyDiscount(10);
    expect(updatedProduct.price.getAmount()).toBe(90);
  });

  test('should throw error when discontinuing a non-available product', () => {
    product = product.markAsOutOfStock();
    expect(() => product.discontinue()).toThrow(InvalidProductStatusException);
  });

  test('should discontinue a product successfully', () => {
    const updatedProduct = product.discontinue();
    expect(updatedProduct.currentStatus).toBe(ProductStatus.Discontinued);
  });

  test('should validate status transition correctly', () => {
    product = product.discontinue();
    expect(() =>
      product.validateStatusTransition(ProductStatus.Available),
    ).not.toThrow();
    expect(() =>
      product.validateStatusTransition(ProductStatus.OutOfStock),
    ).toThrow(InvalidProductStatusException);
  });

  test('should adjust pricing and inventory correctly', () => {
    product = product.restock(20);
    const salesData = { soldUnits: 5, promotionActive: false };
    const updatedProduct = product.adjustPricingAndInventory(salesData);
    expect(updatedProduct.stock).toBe(15);
    expect(updatedProduct.price.getAmount()).toBe(100);
  });

  test('should throw error when adjusting pricing and inventory for non-available product', () => {
    product = product.markAsOutOfStock();
    expect(() =>
      product.adjustPricingAndInventory({
        soldUnits: 1,
        promotionActive: false,
      }),
    ).toThrow(InvalidProductStatusException);
  });

  test('should restock product successfully', () => {
    const updatedProduct = product.restock(10);
    expect(updatedProduct.stock).toBe(10);
  });

  test('should throw error when restocking with non-positive quantity', () => {
    expect(() => product.restock(0)).toThrow(RestockQuantityException);
    expect(() => product.restock(-5)).toThrow(RestockQuantityException);
  });
});
