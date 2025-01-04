import { TestingModule } from '@nestjs/testing';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository';
import { IClearableRepository } from '../types/clearable';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { UserRepository } from 'src/user/infrastructure/repository/user.repository';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { CategoryRepository } from 'src/catalog/infrastructure/repository/category.repository';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import {
  CART_REPOSITORY,
  ORDER_REPOSITORY,
  PAYMENT_REPOSITORY,
  SHIPMENT_REPOSITORY,
} from 'src/checkout/epplication/injection-tokens/repository.token';
import { IOrderRepository } from 'src/checkout/core/repository/order.repository';
import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { IShipmentRepository } from 'src/checkout/core/repository/shipment.repository';

export const clearRepos = async (module: TestingModule) => {
  const repositories = [
    module.get<IProductRepository>(ProductRepository),
    module.get<IUserRepository>(UserRepository),
    module.get<ICategoryRepository>(CategoryRepository),
    module.get<ICartRepository>(CART_REPOSITORY),
    module.get<IShipmentRepository>(SHIPMENT_REPOSITORY),
    module.get<IPaymentRepository>(PAYMENT_REPOSITORY),
    module.get<IOrderRepository>(ORDER_REPOSITORY),
  ];

  for (const repository of repositories) {
    const retypedRepo = repository as unknown as IClearableRepository;
    if (retypedRepo.clear) {
      await retypedRepo.clear();
    }
  }
};
