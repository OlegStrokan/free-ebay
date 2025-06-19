import { Module } from '@nestjs/common';
import { ConfigModule } from '@nestjs/config';
import { ProductIndexingModule } from './product-indexing/product-indexing.module';
import { ElasticsearchConfigModule } from './tools/elastic-search/elastic-search.module';
import { ClientsModule, Transport } from '@nestjs/microservices';

@Module({
  imports: [
    ConfigModule.forRoot({
      envFilePath: `.${process.env.NODE_ENV}.env`,
      isGlobal: true,
    }),
    ClientsModule.register([
      {
        name: 'KAFKA_PRODUCT_PRODUCER',
        transport: Transport.KAFKA,
        options: {
          client: {
            clientId: 'product',
            brokers: ['localhost:9092'],
          },
          consumer: {
            groupId: 'product-consumer',
          },
        },
      },
    ]),
    ElasticsearchConfigModule,
    ProductIndexingModule,
  ],
  providers: [],
})
export class AppModule {}
