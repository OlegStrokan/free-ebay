import { Module } from '@nestjs/common';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { KafkaProducerService } from './kafka-producer.service';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { IKafkaProducerService } from './kafka-producer.interface';

@Module({
  imports: [
    ClientsModule.registerAsync([
      {
        name: 'KAFKA_PRODUCT_PRODUCER',
        imports: [ConfigModule],
        useFactory: (configService: ConfigService) => ({
          transport: Transport.KAFKA,
          options: {
            client: {
              clientId: 'product',
              brokers: ['localhost:9092'],
            },
            producerOnlyMode: true,

            consumer: {
              groupId: 'product-indexing',
            },
          },
        }),
        inject: [ConfigService],
      },
    ]),
  ],
  providers: [
    {
      provide: IKafkaProducerService,
      useClass: KafkaProducerService,
    },
  ],
  exports: [
    {
      provide: IKafkaProducerService,
      useClass: KafkaProducerService,
    },
  ],
})
export class KafkaModule {}
