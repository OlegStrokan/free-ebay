import { Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { IDeleteProductUseCase } from './delete-product.interface';

@Injectable()
export class DeleteProductUseCase implements IDeleteProductUseCase {
  constructor(private readonly productsRepo: IProductRepository) {}

  async execute(id: Product['id']): Promise<void> {
    await this.productsRepo.deleteById(id);
  }
}
