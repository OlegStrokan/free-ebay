// core/src/checkout/core/entity/cart/cart.spec.ts
import { Cart, CartData } from './cart';
import { Money } from 'src/shared/types/money';
import { CartItem } from '../cart-item/cart-item';
import { ICartItemMockService } from '../cart-item/mocks/cart-item-mock.interface';
import { clearRepos } from 'src/shared/testing/clear-repos';
import { createTestingModule } from 'src/shared/testing/test.module';
import { TestingModule } from '@nestjs/testing';
import { CART_ITEM_MOCK_SERVICE } from 'src/checkout/epplication/injection-tokens/mock-services.token';

describe('Cart', () => {
  let cartData: CartData;
  let cart: Cart;
  let cartItemMockService: ICartItemMockService;
  let module: TestingModule;

  beforeAll(async () => {
    module = await createTestingModule();

    cartItemMockService = module.get(CART_ITEM_MOCK_SERVICE);

    await clearRepos(module);
  });

  afterAll(async () => {
    await module.close();
  });

  beforeEach(() => {
    cartData = {
      id: '1',
      userId: 'user1',
      items: [],
      totalPrice: new Money(0, 'USD', 100),
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    cart = new Cart(cartData);
  });

  test('should create a cart successfully', () => {
    const newCart = Cart.create('user2');
    expect(newCart).toBeInstanceOf(Cart);
    expect(newCart.userId).toBe('user2');
    expect(newCart.items).toHaveLength(0);
    expect(newCart.totalPrice.getAmount()).toBe(0);
  });

  test('should add an item successfully', () => {
    const item: CartItem = cartItemMockService.getOne({
      id: 'item1',
      price: new Money(50, 'USD', 100),
    });
    const updatedCart = cart.addItem(item.data);
    expect(updatedCart.items).toHaveLength(1);
    expect(updatedCart.totalPrice.getAmount()).toBe(50);
  });

  test('should remove an item successfully', () => {
    const item: CartItem = cartItemMockService.getOne({
      id: 'item1',
      price: new Money(50, 'USD', 100),
    });
    cart = cart.addItem(item.data);
    const updatedCart = cart.removeItem('item1');
    expect(updatedCart.items).toHaveLength(0);
    expect(updatedCart.totalPrice.getAmount()).toBe(0);
  });

  test('should handle adding multiple items', () => {
    const [item1, item2]: CartItem[] = cartItemMockService.getMany(2, [
      {
        id: 'item1',
        price: new Money(50, 'USD', 100),
      },
      {
        id: 'item2',
        price: new Money(30, 'USD', 100),
      },
    ]);

    const updatedCart = cart.addItem(item1.data);
    const updatedCartWithSecondItem = updatedCart.addItem(item2.data);
    expect(updatedCartWithSecondItem.items).toHaveLength(2);
    expect(updatedCartWithSecondItem.totalPrice.getAmount()).toBe(80);
  });

  test('should clear the cart successfully', () => {
    const item: CartItem = cartItemMockService.getOne({
      id: 'item1',
      price: new Money(50, 'USD', 100),
    });
    cart = cart.addItem(item.data);
    const clearedCart = cart.clearCart();
    expect(clearedCart.items).toHaveLength(0);
    expect(clearedCart.totalPrice.getAmount()).toBe(0);
  });
});
