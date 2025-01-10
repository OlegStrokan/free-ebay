import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { IFindProductUseCase } from './find-product.interface';
import { ProductData } from 'src/product/core/product/entity/product.interface';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';
import { PRODUCT_REPOSITORY } from '../../injection-tokens/repository.token';

@Injectable()
export class FindProductUseCase implements IFindProductUseCase {
  constructor(
    @Inject(PRODUCT_REPOSITORY)
    private readonly productsRepo: IProductRepository,
  ) {}

  async execute(productId: ProductData['id']): Promise<Product> {
    const product = await this.productsRepo.findById(productId);
    if (!product) {
      throw new ProductNotFoundException('id', productId);
    }
    return product;
  }
}
