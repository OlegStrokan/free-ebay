import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { IUseCase } from 'src/shared/types/use-case.interface';
import { PRODUCT_REPOSITORY } from '../../injection-tokens/repository.token';

@Injectable()
export class MarkAsOutOfStockUseCase implements IUseCase<string, Product> {
  constructor(
    @Inject(PRODUCT_REPOSITORY)
    private readonly productsRepo: IProductRepository,
  ) {}

  async execute(id: Product['id']): Promise<Product> {
    const product = await this.productsRepo.findById(id);
    if (!product) {
      throw new ProductNotFoundException('id', id);
    }
    const newProduct = product.markAsOutOfStock();
    return await this.productsRepo.update(newProduct);
  }
}
