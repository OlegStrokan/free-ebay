import { NestFactory } from '@nestjs/core';
import { MicroserviceOptions, Transport } from '@nestjs/microservices';
import { AppModule } from './app.module';

async function bootstrap() {
  const app = await NestFactory.createMicroservice<MicroserviceOptions>(
    AppModule,
    {
      transport: Transport.KAFKA,
      options: {
        client: {
          brokers: ['localhost:9092'],
        },
        consumer: {
          groupId: 'elasticsearch-indexer-consumer-server',
          allowAutoTopicCreation: true,
        },
        subscribe: {
          fromBeginning: true,
        },
      },
    },
  );

  await app.listen();
}
bootstrap();
