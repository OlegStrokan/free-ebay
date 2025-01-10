import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { IFindProductsUseCase } from './find-product.interface';
import { PRODUCT_REPOSITORY } from '../../injection-tokens/repository.token';

@Injectable()
export class FindProductsUseCase implements IFindProductsUseCase {
  constructor(
    @Inject(PRODUCT_REPOSITORY)
    private readonly productsRepo: IProductRepository,
  ) {}

  async execute(): Promise<Product[]> {
    // @notice: temporary shit
    return await this.productsRepo.findAll(1, 100);
  }
}
