import {
  Controller,
  Post,
  Body,
  Get,
  Param,
  NotFoundException,
  Put,
  Delete,
} from '@nestjs/common';

@Controller('products')
export class ProductsController {
  constructor(private readonly productsService: IProductService) {}

  @Post()
  create(@Body() createProductDto: CreateProductDto) {
    return this.productsService.createProduct(createProductDto);
  }

  @Get()
  findAll() {
    return this.productsService.getProducts();
  }

  @Get(':id')
  findOne(@Param('id') id: string) {
    const product = this.productsService.getProductById(id);
    if (!product) throw new NotFoundException('Product not found');
    return product;
  }

  @Put(':id')
  update(@Param('id') id: string, @Body() updateProductDto: UpdateProductDto) {
    const updated = this.productsService.updateProduct(id, updateProductDto);
    if (!updated) throw new NotFoundException('Product not found');
    return updated;
  }

  @Delete(':id')
  remove(@Param('id') id: string) {
    const deleted = this.productsService.deleteProduct(id);
    if (!deleted) throw new NotFoundException('Product not found');
    return { message: 'Product deleted' };
  }
}
