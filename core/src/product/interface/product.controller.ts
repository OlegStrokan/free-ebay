import {
  Controller,
  Post,
  Body,
  UseGuards,
  Get,
  Param,
  Patch,
  Query,
} from '@nestjs/common';
import { AuthGuard } from 'src/auth/interface/guards/auth.guard';
import {
  ApiTags,
  ApiOperation,
  ApiResponse,
  ApiBody,
  ApiParam,
} from '@nestjs/swagger';
import { ICreateProductUseCase } from '../epplication/use-cases/create-product/create-product.interface';
import { IDeleteProductUseCase } from '../epplication/use-cases/delete-product/delete-product.interface';
import { IFindProductUseCase } from '../epplication/use-cases/find-product/find-product.interface';
import { IFindProductsUseCase } from '../epplication/use-cases/find-products/find-product.interface';
import { IMarkAsAvailableUseCase } from '../epplication/use-cases/mark-as-available/mark-as-available.interface';
import { IMarkAsOutOfStockUseCase } from '../epplication/use-cases/mark-as-out-of-stock/mark-as-out-of-stock.interface';
import { ISearchProductsUseCase } from '../epplication/use-cases/search-products/search-products.interface';
import { IGetPriceRangeUseCase } from '../epplication/use-cases/get-price-range/get-price-range.interface';
import { IProductMapper } from '../infrastructure/mappers/product/product.mapper.interface';
import { CreateProductDto } from './dtos/create-product.dto';
import { GetPriceRangeDto } from './dtos/get-price-range.dto';
import { ProductDto } from './dtos/product.dto';

@ApiTags('Products')
@Controller('products')
export class ProductsController {
  constructor(
    private readonly createProductUseCase: ICreateProductUseCase,
    private readonly findProductsUseCase: IFindProductsUseCase,
    private readonly findProductUseCase: IFindProductUseCase,
    private readonly searchProductsUseCase: ISearchProductsUseCase,
    private readonly markAsOutOfStockUseCae: IMarkAsOutOfStockUseCase,
    private readonly markAsAvailableUseCase: IMarkAsAvailableUseCase,
    private readonly deleteProductUseCase: IDeleteProductUseCase,
    private readonly getPriceRangeUseCase: IGetPriceRangeUseCase,
    private readonly mapper: IProductMapper,
  ) {}

  @Post()
  @ApiOperation({ summary: 'Create a new product' })
  @ApiBody({ type: CreateProductDto })
  @ApiResponse({ status: 201, description: 'Product successfully created.' })
  @ApiResponse({ status: 400, description: 'Invalid request payload.' })
  public async create(@Body() dto: CreateProductDto): Promise<void> {
    await this.createProductUseCase.execute(dto);
  }

  @Post('get-price-range')
  @ApiOperation({ summary: 'Get price range analysis for a product' })
  @ApiBody({ type: GetPriceRangeDto })
  @ApiResponse({
    status: 200,
    description: 'Price range analysis successfully retrieved.',
    schema: {
      type: 'object',
      properties: {
        statusCode: { type: 'number', example: 200 },
        message: {
          type: 'array',
          items: { type: 'string' },
          example: ['success'],
        },
        data: {
          type: 'string',
          example:
            'Price Range: $999.99 - $1299.99\nConclusion: REASONABLE\nAnalysis: Based on current market research...',
        },
      },
    },
  })
  @ApiResponse({ status: 400, description: 'Invalid request payload.' })
  public async getPriceRange(@Body() dto: GetPriceRangeDto) {
    return await this.getPriceRangeUseCase.execute(dto);
  }

  // @UseGuards(AuthGuard)
  @Get()
  @ApiOperation({ summary: 'Retrieve all products (cursor-based)' })
  @ApiResponse({
    status: 200,
    description: 'List of products.',
    type: [ProductDto],
  })
  @ApiResponse({ status: 401, description: 'Unauthorized.' })
  public async findAll(
    @Query('after') after?: string,
    @Query('limit') limit = 20,
  ): Promise<{ items: ProductDto[]; nextCursor?: string }> {
    const { items, nextCursor } = await this.findProductsUseCase.execute({
      after,
      limit,
    });
    return {
      items: items.map((product) => this.mapper.toClient(product)),
      nextCursor,
    };
  }

  @Get('/search')
  @ApiOperation({ summary: 'Search products by keyword' })
  @ApiResponse({
    status: 200,
    description: 'Search results.',
    type: [ProductDto],
  })
  public async search(@Query('q') query: string): Promise<ProductDto[]> {
    const products = await this.searchProductsUseCase.execute(query);
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
