import { Injectable } from '@nestjs/common';
import { InjectRepository } from '@nestjs/typeorm';
import { FindManyOptions, MoreThan, Repository } from 'typeorm';
import { Product } from 'src/product/core/product/entity/product';
import { ProductStatus } from 'src/product/core/product/entity/product-status';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { ProductDb } from '../entity/product.entity';
import { ProductData } from 'src/product/core/product/entity/product.interface';
import { IProductMapper } from '../mappers/product/product.mapper.interface';
import { ProductNotFoundException } from 'src/product/core/product/exceptions/product-not-found.exception';
import { ICacheService } from 'src/shared/cache/cache.interface';
import { PaginatedResult } from 'src/shared/types/paginated-result';

@Injectable()
export class ProductRepository implements IProductRepository {
  constructor(
    @InjectRepository(ProductDb)
    private readonly productRepository: Repository<ProductDb>,
    private readonly mapper: IProductMapper,
    private readonly cacheService: ICacheService,
  ) {}

  async save(product: Product): Promise<Product> {
    const productDb = this.mapper.toDb(product);
    const savedProductDb = await this.productRepository.save(productDb);

    const cacheKey = `product:${savedProductDb.id}`;
    const ttl = 300;
    const domainProduct = this.mapper.toDomain(savedProductDb);
    await this.cacheService.set(cacheKey, ttl, domainProduct);
    return domainProduct;
  }
  async findById(id: string): Promise<Product | null> {
    const cacheKey = `product:${id}`;
    const ttl = 300;

    const productDb = await this.cacheService.getOrSet<ProductDb | null>(
      cacheKey,
      ttl,
      async () => {
        return await this.productRepository.findOneBy({ id });
      },
    );

    if (!productDb) {
      return null;
    }

    return this.mapper.toDomain(productDb);
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

  //@non-required-fix: maybe will be better to use date instead of id for scrolling table
  async findAll(
    after?: string,
    limit = 200,
  ): Promise<PaginatedResult<Product>> {
    const findOptions: FindManyOptions<ProductDb> = {
      order: { id: 'ASC' },
      take: limit + 1,
    };

    if (after) {
      findOptions.where = { id: MoreThan(after) };
    }

    const productDbs = await this.productRepository.find(findOptions);

    const hasNextPage = productDbs.length > limit;
    const items = hasNextPage ? productDbs.slice(0, limit) : productDbs;
    const nextCursor = hasNextPage ? items[items.length - 1]?.id : undefined;

    return {
      items: items.map((db) => this.mapper.toDomain(db)),
      nextCursor,
    };
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
    const productExists = await this.productRepository.findOne({
      where: { id: product.id },
      select: ['id'],
    });

    if (!productExists) {
      throw new ProductNotFoundException('id', product.id);
    }

    return this.save(product);
  }

  async discontinue(productData: ProductData): Promise<Product> {
    const foundedProduct = await this.findById(productData.id);
    if (!foundedProduct) {
      throw new ProductNotFoundException('id', productData.id);
    }
    const discontinuedProduct = foundedProduct.discontinue();
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
    await this.productRepository.query(`DELETE FROM "products"`);
  }
}
