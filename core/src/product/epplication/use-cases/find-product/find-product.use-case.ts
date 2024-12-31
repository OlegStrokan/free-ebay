import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository';
import { IFindProductUseCase } from './find-product.interface';
import { ProductData } from 'src/product/core/product/entity/product.interface';

@Injectable()
export class FindProductUseCase implements IFindProductUseCase {
  constructor(
    @Inject(ProductRepository)
    private readonly productsRepo: IProductRepository,
  ) {}

  async execute(productId: ProductData['id']): Promise<Product> {
    return await this.productsRepo.findById(productId);
  }
}
