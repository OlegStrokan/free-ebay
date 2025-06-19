import {
  Injectable,
  Inject,
  OnModuleInit,
  OnModuleDestroy,
  Logger,
} from '@nestjs/common';
import { ClientKafka } from '@nestjs/microservices';
import { Producer } from 'kafkajs';
import { IKafkaProducerService } from './kafka-producer.interface';

@Injectable()
export class KafkaProducerService
  extends IKafkaProducerService
  implements OnModuleInit, OnModuleDestroy
{
  private readonly logger = new Logger(KafkaProducerService.name);
  private producer: Producer | undefined;

  constructor(
    @Inject('KAFKA_PRODUCT_PRODUCER') private readonly clientKafka: ClientKafka,
  ) {
    super();
  }

  async onModuleInit() {
    await this.clientKafka.connect();
    this.logger.log('NestJS ClientKafka connected.');

    this.producer = (this.clientKafka as any).producer as Producer;
    this.logger.log('Kafka producer obtained. Ready for sending messages.');
  }

  async onModuleDestroy() {
    if (this.producer) {
      await this.producer.disconnect();
      this.logger.log('Kafka producer disconnected.');
    }
    await this.clientKafka.close();
    this.logger.log('NestJS ClientKafka closed.');
  }

  async sendMessage(topic: string, message: any, key?: string): Promise<void> {
    if (!this.producer) {
      this.logger.error(
        'Kafka producer is not initialized. Cannot send message.',
      );
      throw new Error('Kafka producer not ready.');
    }
    try {
      await this.producer.send({
        topic,
        messages: [{ key: key || null, value: JSON.stringify(message) }],
        acks: -1,
        timeout: 30000,
      });
      this.logger.debug(
        `Message sent to topic ${topic} (key: ${
          key || 'null'
        }): ${JSON.stringify(message)}`,
      );
    } catch (error: unknown) {
      if (error instanceof Error) {
        this.logger.error(
          `Failed to send message to topic ${topic} (key: ${key || 'null'}): ${
            error.message
          }`,
          error.stack,
        );
      } else {
        this.logger.error(
          `Failed to send message to topic ${topic} (key: ${
            key || 'null'
          }): ${String(error)}`,
        );
      }
      throw error;
    }
  }
}
