import {
  Controller,
  Post,
  Body,
  Inject,
  Get,
  Patch,
  Param,
  Query,
  UseGuards,
} from '@nestjs/common';
import { CreateProductDto } from './dtos/create-product.dto';
import { Product } from '../core/product/entity/product';
import { CreateProductUseCase } from '../epplication/use-cases/create-product/create-product.use-case';
import { FindProductsUseCase } from '../epplication/use-cases/find-products/find-products.use-case';
import { ProductMapper } from '../infrastructure/mappers/product/product.mapper';
import { ProductData } from '../core/product/entity/product.interface';
import { MarkAsAvailableUseCase } from '../epplication/use-cases/mark-as-available/mark-as-available.use-case';
import { MarkAsOutOfStockUseCase } from '../epplication/use-cases/mark-as-out-of-stock/mark-as-out-of-stock.use-case';
import { ICreateProductUseCase } from '../epplication/use-cases/create-product/create-product.interface';
import { IProductMapper } from '../infrastructure/mappers/product/product.mapper.interface';
import { ProductDb } from '../infrastructure/entity/product.entity';
import { IFindProductsUseCase } from '../epplication/use-cases/find-products/find-product.interface';
import { IMarkAsOutOfStockUseCase } from '../epplication/use-cases/mark-as-out-of-stock/mark-as-out-of-stock.interface';
import { IMarkAsAvailableUseCase } from '../epplication/use-cases/mark-as-available/mark-as-available.interface';
import { FindProductUseCase } from '../epplication/use-cases/find-product/find-product.use-case';
import { IFindProductUseCase } from '../epplication/use-cases/find-product/find-product.interface';
import { IDeleteProductUseCase } from '../epplication/use-cases/delete-product/delete-product.interface';
import { DeleteProductUseCase } from '../epplication/use-cases/delete-product/delete-product.use-case';
import { AuthGuard } from 'src/auth/interface/guards/auth.guard';

@Controller('products')
export class ProductsController {
  constructor(
    @Inject(CreateProductUseCase)
    private readonly createProductUseCase: ICreateProductUseCase,
    @Inject(FindProductsUseCase)
    private readonly findProductsUseCase: IFindProductsUseCase,
    @Inject(FindProductUseCase)
    private readonly findProductUseCase: IFindProductUseCase,
    @Inject(MarkAsOutOfStockUseCase)
    private readonly markAsOutOfStockUseCae: IMarkAsOutOfStockUseCase,
    @Inject(MarkAsAvailableUseCase)
    private readonly markAsAvailableUseCase: IMarkAsAvailableUseCase,
    @Inject(DeleteProductUseCase)
    private readonly deleteProductUseCase: IDeleteProductUseCase,
    @Inject(ProductMapper)
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
