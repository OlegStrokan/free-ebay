import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository';
import { IFindProductsUseCase } from './find-product.interface';

@Injectable()
export class FindProductsUseCase implements IFindProductsUseCase {
  constructor(
    @Inject(ProductRepository)
    private readonly productsRepo: IProductRepository,
  ) {}

  async execute(): Promise<Product[]> {
    // @notice: temporary shit
    return await this.productsRepo.findAll(1, 100);
  }
}
