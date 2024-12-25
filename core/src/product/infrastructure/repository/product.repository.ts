import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Product } from 'src/product/core/product/entity/product';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { Money } from 'src/shared/types/money';
import { Repository } from 'typeorm';
import { ProductDb } from '../entity/product.entity';

@Injectable()
export class ProductRepository implements IProductRepository {
  constructor(
    @InjectRepository(ProductDb)
    private readonly productRepository: Repository<ProductDb>,
  ) {}

  async save(product: Product): Promise<Product> {
    const productData = product.data;
    const savedProduct = await this.productRepository.save(productData);
    return new Product(savedProduct);
  }

  async findById(id: string): Promise<Product | null> {
    const productDb = await this.productRepository.findOneBy({ id });
    return productDb;
  }

  async findBySku(sku: string): Promise<Product | null> {
    const productDb = await this.productRepository.findOne({ where: { sku } });
    return productDb;
  }

  async findAll(page: number, limit: number): Promise<Product[]> {
    const [productDbs] = await this.productRepository.findAndCount({
      skip: (page - 1) * limit,
      take: limit,
    });
    return productDbs.map((productDb) => new Product(productDb));
  }

  async deleteById(id: string): Promise<void> {
    await this.productRepository.delete(id);
  }

  async findByStatus(
    status: ProductStatus,
    page: number,
    limit: number,
  ): Promise<Product[]> {
    const [productDbs] = await this.productRepository.findAndCount({
      where: { status },
      skip: (page - 1) * limit,
      take: limit,
    });
    return productDbs.map((productDb) => new Product(productDb));
  }

  async updatePrice(id: string, newPrice: Money): Promise<Product> {
    const product = await this.findById(id);
    if (!product) throw new Error(`Product with id ${id} not found`);

    const updatedProduct = product.updatePrice(newPrice);
    return this.save(updatedProduct);
  }

  async discontinue(id: string): Promise<Product> {
    const product = await this.findById(id);
    if (!product) throw new Error(`Product with id ${id} not found`);

    const discontinuedProduct = product.discontinue();
    return this.save(discontinuedProduct);
  }

  async findByCategory(
    category: string,
    page: number,
    limit: number,
  ): Promise<Product[]> {
    const [productDbs] = await this.productRepository.findAndCount({
      where: { category },
      skip: (page - 1) * limit,
      take: limit,
    });
    return productDbs.map((productDb) => new Product(productDb));
  }

  async findByAvailability(
    isAvailable: boolean,
    page: number,
    limit: number,
  ): Promise<Product[]> {
    const status = isAvailable
      ? ProductStatus.Available
      : ProductStatus.OutOfStock;
    const [productDbs] = await this.productRepository.findAndCount({
      where: { status },
      skip: (page - 1) * limit,
      take: limit,
    });
    return productDbs.map((productDb) => new Product(productDb));
  }
}
