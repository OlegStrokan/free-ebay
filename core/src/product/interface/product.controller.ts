import {
  Controller,
  Post,
  Body,
  Inject,
  Get,
  Patch,
  Param,
} from '@nestjs/common';
import { CreateProductDto } from './dtos/create-product.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';
import { Product } from '../core/product/entity/product';
import { CreateProductUseCase } from '../epplication/use-cases/create-product.use-case';
import { FindProductsUseCase } from '../epplication/use-cases/find-products.use-case';
import { ProductMapper } from '../infrastructure/mappers/product.mapper';
import { ProductData } from '../core/product/entity/product.interface';
import { MarkAsAvailableUseCase } from '../epplication/use-cases/mark-as-available.use-case';
import { MarkAsOutOfStockUseCase } from '../epplication/use-cases/mark-as-out-of-stock.use-case';

@Controller('products')
export class ProductsController {
  constructor(
    @Inject(CreateProductUseCase)
    private readonly createProductUseCase: IUseCase<CreateProductDto, void>,
    @Inject(FindProductsUseCase)
    private readonly findProductsUseCase: IUseCase<null, Product[]>,
    @Inject(MarkAsOutOfStockUseCase)
    private readonly markAsOutOfStockUseCae: IUseCase<string, Product>,
    @Inject(MarkAsAvailableUseCase)
    private readonly markAsAvailableUseCase: IUseCase<string, Product>,
  ) {}

  @Post()
  public async create(@Body() dto: CreateProductDto): Promise<void> {
    await this.createProductUseCase.execute(dto);
  }

  @Get()
  public async findAll(): Promise<ProductData[]> {
    const products = await this.findProductsUseCase.execute();
    return products.map((product) => ProductMapper.toClient(product));
  }

  @Patch('/:id/closed')
  public async markAsOutOfStock(@Param('id') id: string): Promise<ProductData> {
    const product = await this.markAsOutOfStockUseCae.execute(id);
    return ProductMapper.toClient(product);
  }

  @Patch('/:id/open')
  public async markAsAvailable(@Param('id') id: string): Promise<ProductData> {
    const product = await this.markAsAvailableUseCase.execute(id);
    return ProductMapper.toClient(product);
  }
}
