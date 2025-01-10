import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { IDeleteProductUseCase } from './delete-product.interface';
import { PRODUCT_REPOSITORY } from '../../injection-tokens/repository.token';

@Injectable()
export class DeleteProductUseCase implements IDeleteProductUseCase {
  constructor(
    @Inject(PRODUCT_REPOSITORY)
    private readonly productsRepo: IProductRepository,
  ) {}

  async execute(id: Product['id']): Promise<void> {
    await this.productsRepo.deleteById(id);
  }
}
