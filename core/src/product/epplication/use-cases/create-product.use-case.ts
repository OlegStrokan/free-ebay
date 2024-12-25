import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductRepository } from 'src/product/infrastructure/repository/product.repository';
import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

@Injectable()
export class CreateProductUseCase implements IUseCase<CreateProductDto, void> {
  constructor(
    @Inject(ProductRepository)
    private readonly productsRepo: IProductRepository,
  ) {}

  async execute(dto: CreateProductDto): Promise<void> {
    const product = Product.create({ ...dto });
    await this.productsRepo.save(product);
  }
}
