import { Inject, Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { Repository } from 'typeorm';
import { Product } from 'src/product/core/product/entity/product';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductDb } from '../entity/product.entity';
import { ProductData } from 'src/product/core/product/entity/product.interface';
import { IProductMapper } from '../mappers/product/product.mapper.interface';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';
import { FailedToRetrieveProductException } from 'src/product/core/product/exceptions/failed-to-retrieve-product.exception';
import { PRODUCT_MAPPER } from 'src/product/epplication/injection-tokens/mapper.token';
import { ProductDto } from 'src/product/interface/dtos/product.dto';

@Injectable()
export class ProductRepository implements IProductRepository {
  constructor(
    @InjectRepository(ProductDb)
    private readonly productRepository: Repository<ProductDb>,
    @Inject(PRODUCT_MAPPER)
    private readonly mapper: IProductMapper<ProductDto, Product, ProductDb>,
  ) {}

  async save(product: Product): Promise<Product> {
    const productDb = this.mapper.toDb(product);
    await this.productRepository.save(productDb);
    const savedProduct = await this.findById(productDb.id);
    if (!savedProduct) {
      throw new FailedToRetrieveProductException(productDb.id);
    }
    return savedProduct;
  }

  async findById(id: string): Promise<Product | null> {
    const productDb = await this.productRepository.findOneBy({ id });
    return productDb ? this.mapper.toDomain(productDb) : null;
  }

  async findBySku(sku: string): Promise<Product | null> {
    const productDb = await this.productRepository.findOne({ where: { sku } });
    return productDb ? this.mapper.toDomain(productDb) : null;
  }

  async deleteById(id: string): Promise<void> {
    const result = await this.productRepository.delete(id);

    if (result.affected === 0) {
      throw new ProductNotFoundException('id', id);
    }
  }

  async findAll(page: number, limit: number): Promise<Product[]> {
    const [productDbs] = await this.productRepository.findAndCount({
      skip: (page - 1) * limit,
      take: limit,
    });
    return productDbs.map((productDb) => this.mapper.toDomain(productDb));
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
    // TODO update this error handling and return type
    const foundedProduct = await this.findById(product.id);
    if (!foundedProduct) {
      throw new ProductNotFoundException('id', product.id);
    }
    const dbProduct = this.mapper.toDb(product);
    await this.productRepository.update(product.id, dbProduct);
    const updatedProduct = await this.findById(product.id);
    if (!updatedProduct) {
      throw new Error('neco se posralo');
    }
    return updatedProduct;
  }

  async discontinue(productData: ProductData): Promise<Product> {
    const foundedProduct = await this.findById(productData.id);
    if (!foundedProduct) {
      throw new ProductNotFoundException('id', productData.id);
    }
    const discontinuedProduct = foundedProduct.discontinue();
    const updatedProduct = this.save(discontinuedProduct);
    if (!updatedProduct) {
      throw new Error('neco se posralo');
    }
    return updatedProduct;
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
    await this.productRepository.query(`DELETE FROM "products"`);
  }
}
