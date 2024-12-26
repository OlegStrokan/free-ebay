import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository';
import { IUseCase } from 'src/shared/types/use-case.interface';

@Injectable()
export class MarkAsOutOfStockUseCase implements IUseCase<string, Product> {
  constructor(
    @Inject(ProductRepository)
    private readonly productsRepo: IProductRepository,
  ) {}

  async execute(id: Product['id']): Promise<Product> {
    const product = await this.productsRepo.findById(id);
    const newProduct = product.markAsOutOfStock();
    return await this.productsRepo.update(newProduct);
  }
}
