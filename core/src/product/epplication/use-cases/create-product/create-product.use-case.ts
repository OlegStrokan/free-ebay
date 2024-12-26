import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository';
import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { ICreateProductUseCase } from './create-product.interface';

@Injectable()
export class CreateProductUseCase implements ICreateProductUseCase {
  constructor(
    @Inject(ProductRepository)
    private readonly productsRepo: IProductRepository,
  ) {}

  async execute(dto: CreateProductDto): Promise<void> {
    const product = Product.create({ ...dto });
    await this.productsRepo.save(product);
  }
}
