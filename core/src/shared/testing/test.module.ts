import { ConfigModule } from '@nestjs/config';
import { Test } from '@nestjs/testing';
import { TypeOrmModule } from '@nestjs/typeorm';
import { ProductDb } from 'src/product/infrastructure/entity/product.entity';
import { ProductModule } from 'src/product/product.module';
import { UserModule } from 'src/user/user.module';
import { AuthModule } from 'src/auth/auth.module';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';
import { CatalogModule } from 'src/catalog/catalog.module';
import { CategoryDb } from 'src/catalog/infrastructure/entity/category.entity';
import { CheckoutModule } from 'src/checkout/checkout.module';
import { CartDb } from 'src/checkout/infrastructure/entity/cart.entity';
import { CartItemDb } from 'src/checkout/infrastructure/entity/cart-item.entity';
import { ShipmentDb } from 'src/checkout/infrastructure/entity/shipment.entity';
import { OrderItemDb } from 'src/checkout/infrastructure/entity/order-item.entity';
import { PaymentDb } from 'src/checkout/infrastructure/entity/payment.entity';
import { OrderDb } from 'src/checkout/infrastructure/entity/order.entity';
import { DatabaseModule } from './database/database.module';
import { authProviders } from 'src/auth/auth.providers';
import { userProviders } from 'src/user/user.provider';
import { checkoutProviders } from 'src/checkout/checkout.providers';
import { productProviders } from 'src/product/product.provider';
import { HttpService } from '@nestjs/axios';
import { of } from 'rxjs';
import { KafkaModule } from '../kafka/kafka.module';
import { CacheModule } from '../cache/cache.module';
import { AiChatBotModule } from 'src/ai-chatbot/ai-chatbot.module';
import { LangchainChatService } from 'src/ai-chatbot/services/langchain-chat/langchain-chat.service';

export const createTestingModule = async () => {
  return await Test.createTestingModule({
    imports: [
      ConfigModule.forRoot({
        envFilePath: `.${process.env.NODE_ENV}.env`,
        isGlobal: true,
      }),
      TypeOrmModule.forFeature([
        ProductDb,
        UserDb,
        CategoryDb,
        CartDb,
        PaymentDb,
        ShipmentDb,
        OrderDb,
        OrderItemDb,
        CartItemDb,
      ]),
      DatabaseModule,
      ProductModule,
      AuthModule,
      UserModule,
      CatalogModule,
      CheckoutModule,
      KafkaModule,
      CacheModule,
      KafkaModule,
      AiChatBotModule,
    ],
    exports: [],
    providers: [
      ...authProviders,
      ...userProviders,
      ...checkoutProviders,
      ...productProviders,
      {
        provide: LangchainChatService,
        useValue: {
          agentChat: jest
            .fn()
            .mockResolvedValue('Mocked AI response for price range.'),
        },
      },

      {
        provide: HttpService,
        useValue: {
          get: jest.fn(() =>
            of({
              data: {},
              status: 200,
              statusText: 'OK',
              headers: {},
              config: {},
            }),
          ),
          post: jest.fn(() =>
            of({
              data: {},
              status: 200,
              statusText: 'OK',
              headers: {},
              config: {},
            }),
          ),
          put: jest.fn(() =>
            of({
              data: {},
              status: 200,
              statusText: 'OK',
              headers: {},
              config: {},
            }),
          ),
          delete: jest.fn(() =>
            of({
              data: {},
              status: 200,
              statusText: 'OK',
              headers: {},
              config: {},
            }),
          ),
        },
      },
    ],
  }).compile();
};
