import { Injectable, OnModuleInit, Logger } from '@nestjs/common';
import { ElasticsearchService } from '@nestjs/elasticsearch';

interface ProductKafkaEventPayload {
  eventType: 'productCreated' | 'productUpdated' | 'productDeleted';
  productId: string;
  name: string;
  description: string;
  sku: string;
  price: { currency: string; amount: number };
  createdAt?: string;
  updatedAt?: string;
}

@Injectable()
export class ProductIndexingService implements OnModuleInit {
  private readonly PRODUCTS_INDEX_NAME = 'products';

  constructor(
    private readonly elasticsearchService: ElasticsearchService,
    private readonly logger: Logger,
  ) {}

  async onModuleInit() {
    await this.ensureProductIndexExists();
    this.logger.debug('ProductIndexingService initialized successfully');
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
      throw error; // Re-throw to prevent service from starting with broken ES connection
    }
  }

  // todo-later: refactor this switch case
  public async handleProductEvent(message: ProductKafkaEventPayload) {
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
          });
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
          break;

        case 'productDeleted':
          await this.elasticsearchService.delete({
            index: this.PRODUCTS_INDEX_NAME,
            id: productId,
          });

          break;

        default:
          this.logger.warn(`Unknown event type received: ${eventType}`);
      }
    } catch (error: unknown) {
      this.logger.error(
        `Failed to process event for product ${productId} (${eventType}): ${String(
          error,
        )}`,
      );
      throw error; // Re-throw to let Kafka handle retry logic
    }
  }
}
