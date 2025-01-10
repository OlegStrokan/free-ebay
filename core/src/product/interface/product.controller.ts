import {
  Controller,
  Inject,
  Post,
  Body,
  UseGuards,
  Get,
  Param,
  Patch,
} from '@nestjs/common';
import { AuthGuard } from 'src/auth/interface/guards/auth.guard';
import { Product } from '../core/product/entity/product';
import { ProductData } from '../core/product/entity/product.interface';
import { PRODUCT_MAPPER } from '../epplication/injection-tokens/mapper.token';
import {
  CREATE_PRODUCT_USE_CASE,
  FIND_PRODUCTS_USE_CASE,
  FIND_PRODUCT_USE_CASE,
  MARK_AS_OUT_OF_STOCK_USE_CASE,
  MARK_AS_AVAILABLE_USE_CASE,
  DELETE_PRODUCT_USE_CASE,
} from '../epplication/injection-tokens/use-case.token';
import { ICreateProductUseCase } from '../epplication/use-cases/create-product/create-product.interface';
import { IDeleteProductUseCase } from '../epplication/use-cases/delete-product/delete-product.interface';
import { IFindProductUseCase } from '../epplication/use-cases/find-product/find-product.interface';
import { IFindProductsUseCase } from '../epplication/use-cases/find-products/find-product.interface';
import { IMarkAsAvailableUseCase } from '../epplication/use-cases/mark-as-available/mark-as-available.interface';
import { IMarkAsOutOfStockUseCase } from '../epplication/use-cases/mark-as-out-of-stock/mark-as-out-of-stock.interface';
import { ProductDb } from '../infrastructure/entity/product.entity';
import { IProductMapper } from '../infrastructure/mappers/product/product.mapper.interface';
import { CreateProductDto } from './dtos/create-product.dto';

@Controller('products')
export class ProductsController {
  constructor(
    @Inject(CREATE_PRODUCT_USE_CASE)
    private readonly createProductUseCase: ICreateProductUseCase,
    @Inject(FIND_PRODUCTS_USE_CASE)
    private readonly findProductsUseCase: IFindProductsUseCase,
    @Inject(FIND_PRODUCT_USE_CASE)
    private readonly findProductUseCase: IFindProductUseCase,
    @Inject(MARK_AS_OUT_OF_STOCK_USE_CASE)
    private readonly markAsOutOfStockUseCae: IMarkAsOutOfStockUseCase,
    @Inject(MARK_AS_AVAILABLE_USE_CASE)
    private readonly markAsAvailableUseCase: IMarkAsAvailableUseCase,
    @Inject(DELETE_PRODUCT_USE_CASE)
    private readonly deleteProductUseCase: IDeleteProductUseCase,
    @Inject(PRODUCT_MAPPER)
    private readonly mapper: IProductMapper<ProductData, Product, ProductDb>,
  ) {}

  @Post()
  public async create(@Body() dto: CreateProductDto): Promise<void> {
    await this.createProductUseCase.execute(dto);
  }

  @UseGuards(AuthGuard)
  @Get()
  public async findAll(): Promise<ProductData[]> {
    const products = await this.findProductsUseCase.execute();
    return products.map((product) => this.mapper.toClient(product));
  }

  @Get(':id')
  public async findOne(@Param('id') id: string): Promise<ProductData> {
    const product = await this.findProductUseCase.execute(id);
    return this.mapper.toClient(product);
  }

  @Patch(':id/closed')
  public async markAsOutOfStock(@Param('id') id: string): Promise<ProductData> {
    const product = await this.markAsOutOfStockUseCae.execute(id);
    return this.mapper.toClient(product);
  }

  @Patch(':id/open')
  public async markAsAvailable(@Param('id') id: string): Promise<ProductData> {
    const product = await this.markAsAvailableUseCase.execute(id);
    return this.mapper.toClient(product);
  }

  @Patch(':id/delete')
  public async delete(@Param('id') id: string): Promise<void> {
    await this.deleteProductUseCase.execute(id);
  }
}
