import { Controller, Logger } from '@nestjs/common';
import {
  EventPattern,
  Payload,
  Ctx,
  KafkaContext,
} from '@nestjs/microservices';
import { ProductIndexingService } from './product-indexing.service';

@Controller()
export class ProductIndexingController {
  private readonly logger = new Logger(ProductIndexingController.name);

  constructor(
    private readonly productIndexingService: ProductIndexingService,
  ) {}

  @EventPattern('product-events')
  async handleProductEvent(
    @Payload() message: any,
    @Ctx() context: KafkaContext,
  ) {
    this.logger.log(`Received product event: ${JSON.stringify(message)}`);
    await this.productIndexingService.handleProductEvent(message, context);
  }
}
