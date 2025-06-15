import { Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { IMarkAsAvailableUseCase } from './mark-as-available.interface';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';

@Injectable()
export class MarkAsAvailableUseCase implements IMarkAsAvailableUseCase {
  constructor(private readonly productsRepo: IProductRepository) {}

  async execute(id: Product['id']): Promise<Product> {
    const product = await this.productsRepo.findById(id);
    if (!product) {
      throw new ProductNotFoundException('id', id);
    }
    const newProduct = product.markAsAvailable();
    return await this.productsRepo.update(newProduct);
  }
}
