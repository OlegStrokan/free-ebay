import { Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { IMarkAsOutOfStockUseCase } from './mark-as-out-of-stock.interface';

@Injectable()
export class MarkAsOutOfStockUseCase implements IMarkAsOutOfStockUseCase {
  constructor(private readonly productsRepo: IProductRepository) {}

  async execute(id: Product['id']): Promise<Product> {
    const product = await this.productsRepo.findById(id);
    if (!product) {
      throw new ProductNotFoundException('id', id);
    }
    const newProduct = product.markAsOutOfStock();
    return await this.productsRepo.update(newProduct);
  }
}
