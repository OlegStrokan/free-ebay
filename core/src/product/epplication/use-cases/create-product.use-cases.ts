import { Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

@Injectable()
export class CreateProductUseCase
  implements IUseCase<CreateProductDto, Product>
{
  constructor(private readonly productsRepo: IProductRepository) {}

  async execute(dto: CreateProductDto): Promise<Product> {
    const product = Product.create({ ...dto });
    return this.productsRepo.save(product);
  }
}
