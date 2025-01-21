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
import {
  ApiTags,
  ApiOperation,
  ApiResponse,
  ApiBody,
  ApiParam,
} from '@nestjs/swagger';
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
import { ProductDto } from './dtos/product.dto';

@ApiTags('Products')
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
    private readonly mapper: IProductMapper<ProductDto, Product, ProductDb>,
  ) {}

  @Post()
  @ApiOperation({ summary: 'Create a new product' })
  @ApiBody({ type: CreateProductDto })
  @ApiResponse({ status: 201, description: 'Product successfully created.' })
  @ApiResponse({ status: 400, description: 'Invalid request payload.' })
  public async create(@Body() dto: CreateProductDto): Promise<void> {
    await this.createProductUseCase.execute(dto);
  }

  @UseGuards(AuthGuard)
  @Get()
  @ApiOperation({ summary: 'Retrieve all products' })
  @ApiResponse({
    status: 200,
    description: 'List of products successfully retrieved.',
    type: [ProductDto],
  })
  @ApiResponse({ status: 401, description: 'Unauthorized.' })
  public async findAll(): Promise<ProductDto[]> {
    const products = await this.findProductsUseCase.execute();
    return products.map((product) => this.mapper.toClient(product));
  }

  @Get(':id')
  @ApiOperation({ summary: 'Retrieve a product by ID' })
  @ApiParam({ name: 'id', description: 'Product ID', type: String })
  @ApiResponse({
    status: 200,
    description: 'Product successfully retrieved.',
    type: ProductDto,
  })
  @ApiResponse({ status: 404, description: 'Product not found.' })
  public async findOne(@Param('id') id: string): Promise<ProductDto> {
    const product = await this.findProductUseCase.execute(id);
    return this.mapper.toClient(product);
  }

  @Patch(':id/closed')
  @ApiOperation({ summary: 'Mark a product as out of stock' })
  @ApiParam({ name: 'id', description: 'Product ID', type: String })
  @ApiResponse({
    status: 200,
    description: 'Product marked as out of stock.',
    type: ProductDto,
  })
  @ApiResponse({ status: 404, description: 'Product not found.' })
  public async markAsOutOfStock(@Param('id') id: string): Promise<ProductDto> {
    const product = await this.markAsOutOfStockUseCae.execute(id);
    return this.mapper.toClient(product);
  }

  @Patch(':id/open')
  @ApiOperation({ summary: 'Mark a product as available' })
  @ApiParam({ name: 'id', description: 'Product ID', type: String })
  @ApiResponse({
    status: 200,
    description: 'Product marked as available.',
    type: ProductDto,
  })
  @ApiResponse({ status: 404, description: 'Product not found.' })
  public async markAsAvailable(@Param('id') id: string): Promise<ProductDto> {
    const product = await this.markAsAvailableUseCase.execute(id);
    return this.mapper.toClient(product);
  }

  @Patch(':id/delete')
  @ApiOperation({ summary: 'Delete a product' })
  @ApiParam({ name: 'id', description: 'Product ID', type: String })
  @ApiResponse({ status: 204, description: 'Product successfully deleted.' })
  @ApiResponse({ status: 404, description: 'Product not found.' })
  public async delete(@Param('id') id: string): Promise<void> {
    await this.deleteProductUseCase.execute(id);
  }
}
