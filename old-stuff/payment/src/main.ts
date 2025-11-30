import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { MicroserviceOptions, Transport } from '@nestjs/microservices';

async function bootstrap() {
  const app = await NestFactory.createMicroservice<MicroserviceOptions>(
    AppModule,
    {
      transport: Transport.GRPC,
      options: {
        package: 'payment',
        protoPath: 'protos/payment.proto',
        url: '0.0.0.0:50051',
      },
    },
  );

  app.listen();
  console.log('🚀 Payment microservice is running on gRPC port 50051');
}
bootstrap();
