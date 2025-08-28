import { Module } from '@nestjs/common';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { join } from 'path';
import { PaymentGrpcService } from './payment-grpc.service';

@Module({
  imports: [
    ClientsModule.register([
      {
        name: 'PAYMENT_PACKAGE',
        transport: Transport.GRPC,
        options: {
          package: 'payment',
          protoPath: join(__dirname, 'proto/payment.proto'),
          url: process.env.PAYMENT_GRPC_URL || 'localhost:50051',
        },
      },
    ]),
  ],
  providers: [PaymentGrpcService],
  exports: [PaymentGrpcService],
})
export class GrpcModule {}
