import { TestingModule } from '@nestjs/testing';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { IClearableRepository } from '../types/clearable';
import { IUserRepository } from 'src/user/core/repository/user.repository';
import { ICategoryRepository } from 'src/catalog/core/category/repository/category.repository';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';

import { IOrderRepository } from 'src/checkout/core/repository/order.repository';
import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { IShipmentRepository } from 'src/checkout/core/repository/shipment.repository';

export const clearRepos = async (module: TestingModule) => {
  const repositories = [
    module.get(IProductRepository),
    module.get(IUserRepository),
    module.get(ICategoryRepository),
    module.get(ICartRepository),
    module.get(IShipmentRepository),
    module.get(IPaymentRepository),
    module.get(IOrderRepository),
  ];

  for (const repository of repositories) {
    const retypedRepo = repository as unknown as IClearableRepository;
    if (retypedRepo.clear) {
      await retypedRepo.clear();
    }
  }
};
