import { Injectable, OnModuleInit, Logger } from '@nestjs/common';
import { ElasticsearchService } from '@nestjs/elasticsearch';
import { EventPattern, Payload } from '@nestjs/microservices';

interface ProductKafkaEventPayload {
  eventType: 'productCreated' | 'productUpdated' | 'productDeleted';
  productId: string;
  name: string;
  description: string;
  sku: string;
  price: number;
  createdAt?: string;
  updatedAt?: string;
}

@Injectable()
export class ProductIndexingService implements OnModuleInit {
  private readonly logger = new Logger(ProductIndexingService.name);
  private readonly PRODUCTS_INDEX_NAME = 'products';

  constructor(private readonly elasticsearchService: ElasticsearchService) {}

  async onModuleInit() {
    await this.ensureProductIndexExists();
  }

  private async ensureProductIndexExists(): Promise<void> {
    try {
      const indexExists = await this.elasticsearchService.indices.exists({
        index: this.PRODUCTS_INDEX_NAME,
      });

      if (!indexExists) {
        this.logger.log(
          `Elasticsearch index '${this.PRODUCTS_INDEX_NAME}' does not exist. Creating...`,
        );
        await this.elasticsearchService.indices.create({
          index: this.PRODUCTS_INDEX_NAME,
        });
        this.logger.log(
          `Elasticsearch index '${this.PRODUCTS_INDEX_NAME}' created successfully.`,
        );
      } else {
        this.logger.log(
          `Elasticsearch index '${this.PRODUCTS_INDEX_NAME}' already exists.`,
        );
      }
    } catch (error: unknown) {
      if (error instanceof Error) {
        this.logger.error(
          `Failed to ensure Elasticsearch index exists: ${error.message}`,
          error.stack,
        );
      } else {
        this.logger.error(
          `Failed to ensure Elasticsearch index exists: ${String(error)}`,
        );
      }
    }
  }

  @EventPattern('product-events')
  async handleProductEvent(@Payload() message: ProductKafkaEventPayload) {
    this.logger.log(`Received product event: ${JSON.stringify(message)}`);

    const { eventType, productId, ...productData } = message;

    try {
      switch (eventType) {
        case 'productCreated':
          await this.elasticsearchService.index({
            index: this.PRODUCTS_INDEX_NAME,
            id: productId,
            document: {
              productId,
              name: productData.name,
              description: productData.description,
              sku: productData.sku,
              price: productData.price,
              createdAt: productData.createdAt,
              updatedAt: productData.updatedAt,
            },
            op_type: 'index',
          });
          this.logger.log(`Product ${productId} indexed/updated successfully.`);
          break;

        case 'productUpdated':
          await this.elasticsearchService.update({
            index: this.PRODUCTS_INDEX_NAME,
            id: productId,
            doc: {
              name: productData.name,
              description: productData.description,
              sku: productData.sku,
              price: productData.price,
              updatedAt: productData.updatedAt,
            },
          });
          this.logger.log(`Product ${productId} updated successfully.`);
          break;

        case 'productDeleted':
          await this.elasticsearchService.delete({
            index: this.PRODUCTS_INDEX_NAME,
            id: productId,
          });
          this.logger.log(
            `Product ${productId} deleted successfully from Elasticsearch.`,
          );
          break;

        default:
          this.logger.warn(`Unknown event type received: ${eventType}`);
      }
    } catch (error: unknown) {
      if (error instanceof Error) {
        this.logger.error(
          `Failed to process event for product ${productId} (${eventType}): ${error.message}`,
          error.stack,
        );
      } else {
        this.logger.error(
          `Failed to process event for product ${productId} (${eventType}): ${String(
            error,
          )}`,
        );
      }
    }
  }
}
