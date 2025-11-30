import { Injectable } from '@nestjs/common';
import { Product } from 'src/product/core/product/entity/product';
import { IProductRepository } from 'src/product/core/product/repository/product.repository';
import { CreateProductDto } from 'src/product/interface/dtos/create-product.dto';
import { ICreateProductUseCase } from './create-product.interface';
import { ProductAlreadyExistsException } from 'src/product/core/product/exceptions/product-already-exists.exception';
import { IKafkaProducerService } from 'src/shared/kafka/kafka-producer.interface';
import { Money } from 'src/shared/types/money';
import { Ulid } from 'src/shared/types/types';

// Define the structure of the message payload for better type safety
interface ProductKafkaEvent {
  eventType: 'productCreated' | 'productUpdated' | 'productDeleted';
  productId: Ulid;
  name: string;
  description: string;
  sku: string;
  price: Money;
  createdAt?: Date;
  updatedAt?: Date;
}

// @discuss - maybe we should remove logic of preparing event for kafka and sending it to kafka into some publisher service
@Injectable()
export class CreateProductUseCase implements ICreateProductUseCase {
  constructor(
    private readonly productsRepo: IProductRepository,
    private readonly kafkaProducerService: IKafkaProducerService,
  ) {}

  async execute(dto: CreateProductDto): Promise<void> {
    const existedProduct = await this.productsRepo.findBySku(dto.sku);
    if (existedProduct) {
      throw new ProductAlreadyExistsException(dto.sku);
    }
    const product = Product.create({ ...dto });
    await this.productsRepo.save(product);

    //@non-required-fix: create eventType enum + kafka topics enum
    const productEvent: ProductKafkaEvent = {
      eventType: 'productCreated',
      productId: product.id,
      name: product.name,
      description: product.description,
      sku: product.sku,
      price: product.price,
      createdAt: product.createdAt,
      updatedAt: product.updatedAt,
    };

    await this.kafkaProducerService.sendMessage(
      'product-events',
      productEvent,
      product.id,
    );
  }
}
