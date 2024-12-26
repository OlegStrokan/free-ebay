import { TestingModule } from '@nestjs/testing';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository';
import { IClearableRepository } from '../types/clearable';

export const clearRepos = async (module: TestingModule) => {
  const repositories = [module.get<IProductRepository>(ProductRepository)];

  for (const repository of repositories) {
    const retypedRepo = repository as unknown as IClearableRepository;
    if (retypedRepo.clear) {
      await retypedRepo.clear();
    }
  }
};
