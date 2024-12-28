import { Inject, Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { Product } from 'src/product/core/product/entity/product';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductDb } from '../entity/product.entity';
import { ProductData } from 'src/product/core/product/entity/product.interface';
import { ProductNotFoundError } from 'src/product/core/product/error';
import { IProductMapper } from '../mappers/product/product.mapper.interface';
import { ProductMapper } from '../mappers/product/product.mapper';

@Injectable()
export class ProductRepository implements IProductRepository {
  constructor(
    @InjectRepository(ProductDb)
    private readonly productRepository: Repository<ProductDb>,
    @Inject(ProductMapper)
    private readonly mapper: IProductMapper<ProductData, Product, ProductDb>,
  ) {}

  async save(product: Product): Promise<Product> {
    const productDb = this.mapper.toDb(product);
    await this.productRepository.save(productDb);
    return await this.findById(productDb.id);
  }

  async findById(id: string): Promise<Product | null> {
    const productDb = await this.productRepository.findOneBy({ id });
    if (!productDb) {
      throw new ProductNotFoundError(`Product with id ${id} not found`);
    }
    return this.mapper.toDomain(productDb);
  }

  async findBySku(sku: string): Promise<Product | null> {
    const productDb = await this.productRepository.findOne({ where: { sku } });
    return this.mapper.toDomain(productDb);
  }

  async findAll(page: number, limit: number): Promise<Product[]> {
    const [productDbs] = await this.productRepository.findAndCount({
      skip: (page - 1) * limit,
      take: limit,
    });
    return productDbs.map((productDb) => this.mapper.toDomain(productDb));
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
    return productDbs.map((productDb) => this.mapper.toDomain(productDb));
  }

  async update(product: Product): Promise<Product> {
    await this.findById(product.id);
    const dbProduct = this.mapper.toDb(product);
    await this.productRepository.update(product.id, dbProduct);
    return await this.findById(product.id);
  }

  async discontinue(id: string): Promise<Product> {
    const product = await this.findById(id);
    const discontinuedProduct = product.discontinue();
    return this.save(discontinuedProduct);
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
    return productDbs.map((productDb) => this.mapper.toDomain(productDb));
  }

  async clear(): Promise<void> {
    await this.productRepository.clear();
  }
}
