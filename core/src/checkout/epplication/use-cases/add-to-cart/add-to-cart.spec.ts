import { TestingModule } from '@nestjs/testing';
import { createTestingModule } from 'src/shared/testing/test.module';
import { ADD_TO_CART_USE_CASE_TOKEN } from '../../injection-tokens/use-case.token';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { ICartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.interface';
import { CartMockService } from 'src/checkout/core/entity/cart/mocks/cart-mock.service';
import { IAddToCartUseCase } from './add-to-cart.interface';
import { ICartItemMockService } from 'src/checkout/core/entity/cart-item/cart-item-mock.interface';
import { CartItemMockService } from 'src/checkout/core/entity/cart-item/cart-item-mock.service';
import { IProductMockService } from 'src/product/core/product/entity/mocks/product-mock.interface';
import { ProductMockService } from 'src/product/core/product/entity/mocks/product-mock.service';

describe('CreateCartUseCase', () => {
  let addToCartUseCase: IAddToCartUseCase;
  let productMockService: IProductMockService;
  let cartMockService: ICartMockService;
  let cartItemMockService: ICartItemMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    addToCartUseCase = module.get(ADD_TO_CART_USE_CASE_TOKEN);
    cartMockService = module.get(CartMockService);
    cartItemMockService = module.get(CartItemMockService);
    productMockService = module.get(ProductMockService);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  it('should successfully add item to cart and verify it existence', async () => {
    const userId = generateUlid();
    const cartId = generateUlid();
    const product = await productMockService.createOne();
    await cartMockService.createOne({ id: cartId, userId, items: [] });
    const cartItem = cartItemMockService.getOneToCreate({
      cartId: cartId,
      productId: product.id,
    });

    const cartWithItem = await addToCartUseCase.execute(cartItem);

    expect(cartWithItem).toBeDefined();
    expect(cartWithItem.items).toBeDefined();
    expect(cartWithItem.items[0].productId).toBe(product.id);
    expect(cartWithItem.items[0].quantity).toBe(1);
    expect(cartWithItem.items[0].price).toStrictEqual(product.price);
    expect(cartWithItem.totalPrice).toStrictEqual(product.price);
  }, 10000000);

  // it('should throw exception because cart already exist', async () => {
  //   const userId = generateUlid();
  //   await userMockService.createOne({ id: userId });
  //   await cartMockService.createOne({ userId });

  //   await expect(addToCartUseCase.execute({ userId })).rejects.toThrow(
  //     CartAlreadyExists,
  //   );
  // });

  // it("should throw exception because user doesn't exist", async () => {
  //   const userId = generateUlid();

  //   await expect(addToCartUseCase.execute({ userId })).rejects.toThrow(
  //     UserNotFoundException,
  //   );
  // });
});
