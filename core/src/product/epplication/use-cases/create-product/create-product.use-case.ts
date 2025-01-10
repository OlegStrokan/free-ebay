import { Inject, Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { ICreateProductUseCase } from './create-product.interface';
import { ProductAlreadyExistsException } from 'src/product/core/product/exceptions/product-already-exists.exception';
import { PRODUCT_REPOSITORY } from '../../injection-tokens/repository.token';

@Injectable()
export class CreateProductUseCase implements ICreateProductUseCase {
  constructor(
    @Inject(PRODUCT_REPOSITORY)
    private readonly productsRepo: IProductRepository,
  ) {}

  async execute(dto: CreateProductDto): Promise<void> {
    const existedProduct = await this.productsRepo.findBySku(dto.sku);
    if (existedProduct) {
      throw new ProductAlreadyExistsException(dto.sku);
    }
    const product = Product.create({ ...dto });
    await this.productsRepo.save(product);
  }
}
