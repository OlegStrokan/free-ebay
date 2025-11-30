import { Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { IFindProductsUseCase } from './find-product.interface';
import { PaginatedResult } from 'src/shared/types/paginated-result';

export interface FindProductsRequestDto {
  after?: string;
  limit?: number;
}

@Injectable()
export class FindProductsUseCase implements IFindProductsUseCase {
  constructor(private readonly productsRepo: IProductRepository) {}

  async execute(
    dto: FindProductsRequestDto,
  ): Promise<PaginatedResult<Product>> {
    return this.productsRepo.findAll(dto.after, dto.limit);
  }
}
