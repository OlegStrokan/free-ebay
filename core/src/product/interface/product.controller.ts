import { Controller, Post, Body, Inject, Get } from '@nestjs/common';
import { CreateProductDto } from './dtos/create-product.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';
import { Product } from '../core/product/entity/product';
import { CreateProductUseCase } from '../epplication/use-cases/create-product.use-case';
import { FindProductsUseCase } from '../epplication/use-cases/find-products.use-case';

@Controller('products')
export class ProductsController {
  constructor(
    @Inject(CreateProductUseCase)
    private readonly createProductUseCase: IUseCase<CreateProductDto, void>,
    @Inject(FindProductsUseCase)
    private readonly findProductsUseCase: IUseCase<null, Product[]>,
  ) {}

  @Post()
  public async create(@Body() dto: CreateProductDto): Promise<void> {
    return await this.createProductUseCase.execute(dto);
  }

  @Get()
  public async findAll(): Promise<Product[]> {
    return await this.findProductsUseCase.execute();
  }
}
