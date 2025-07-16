import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { IAddToCartUseCase } from './add-to-cart.interface';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';
import { ICartItemMockService } from 'src/checkout/core/entity/cart-item/mocks/cart-item-mock.interface';

describe('CreateCartUseCase', () => {
  let addToCartUseCase: IAddToCartUseCase;
  let productMockService: IProductMockService;
  let cartMockService: ICartMockService;
  let cartItemMockService: ICartItemMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    addToCartUseCase = module.get(IAddToCartUseCase);
    cartMockService = module.get(ICartMockService);
    cartItemMockService = module.get(ICartItemMockService);
    productMockService = module.get(IProductMockService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully add item to cart and recalculate total price', async () => {
    const userId = generateUlid();
    const cartId = generateUlid();
    const product = await productMockService.createOne();
    console.log('product price ', product.price);

    await cartMockService.createOne({ id: cartId, userId, items: [] });
    const cartItem = cartItemMockService.getOneToCreate({
      cartId: cartId,
      productId: product.id,
      quantity: 1,
    });
    const cartWithItem = await addToCartUseCase.execute(cartItem);

    expect(cartWithItem).toBeDefined();
    expect(cartWithItem.items).toBeDefined();
    expect(cartWithItem.items[0].productId).toBe(product.id);
    expect(cartWithItem.items[0].quantity).toBe(1);
    console.log(
      cartWithItem.items[0].price,
      product.price,
      cartWithItem.totalPrice,
    );
    expect(cartWithItem.items[0].price).toStrictEqual(product.price);
    expect(cartWithItem.totalPrice).toStrictEqual(product.price);
  });

  it.skip('should throw exception because cart not found', async () => {
    const cartItem = cartItemMockService.getOneToCreate();
    await expect(addToCartUseCase.execute(cartItem)).rejects.toThrow(
      CartNotFoundException,
    );
  });

  it.skip("should throw exception because product doesn't exist", async () => {
    const userId = generateUlid();
    const cartId = generateUlid();

    await cartMockService.createOne({ id: cartId, userId, items: [] });
    const cartItem = cartItemMockService.getOneToCreate({
      cartId: cartId,
    });

    await expect(addToCartUseCase.execute(cartItem)).rejects.toThrow(
      ProductNotFoundException,
    );
  });
});
