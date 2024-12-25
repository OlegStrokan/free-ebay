import { Controller, Post, Body } from '@nestjs/common';
import { CreateProductDto } from './dtos/create-product.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';
import { Product } from '../core/product/entity/product';

@Controller('products')
export class ProductsController {
  constructor(
    private readonly createProductUseCase: IUseCase<CreateProductDto, Product>,
  ) {}

  @Post()
  create(@Body() dto: CreateProductDto) {
    return this.createProductUseCase.execute(dto);
  }
}
