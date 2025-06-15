import { Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { IFindProductsUseCase } from './find-product.interface';

@Injectable()
export class FindProductsUseCase implements IFindProductsUseCase {
  constructor(private readonly productsRepo: IProductRepository) {}

  async execute(): Promise<Product[]> {
    // @fix: temporary shit
    return await this.productsRepo.findAll(1, 100);
  }
}
