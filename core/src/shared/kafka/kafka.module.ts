import { Module } from '@nestjs/common';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { KafkaProducerService } from './kafka-producer.service';
import { IKafkaProducerService } from './kafka-producer.interface';

@Module({
  imports: [
    ClientsModule.register([
      {
        name: 'KAFKA_PRODUCT_PRODUCER',
        transport: Transport.KAFKA,

        options: {
          client: {
            clientId: 'product',
            brokers: ['localhost:9092'],
            retry: {
              initialRetryTime: 300,
              maxRetryTime: 30000,
              retries: 10,
              factor: 0.2,
            },
          },
          consumer: {
            groupId: 'product-consumer',
          },
        },
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
