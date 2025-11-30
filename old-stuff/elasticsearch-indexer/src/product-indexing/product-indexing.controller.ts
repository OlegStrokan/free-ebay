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
  constructor(
    private readonly productIndexingService: ProductIndexingService,
    private readonly logger: Logger,
  ) {}

  @EventPattern('product-events')
  async handleProductEvent(
    @Payload() message: any,
    // @Ctx() context: KafkaContext, // @discuss - maybe we should use this context to get the partition and offset
  ) {
    this.logger.debug(`Received product event: ${JSON.stringify(message)}`);
    await this.productIndexingService.handleProductEvent(message);
  }
}
