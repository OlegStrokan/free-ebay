import { ConfigModule } from '@nestjs/config';
import { Test, TestingModuleBuilder } from '@nestjs/testing';
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
import { authProviders } from 'src/auth/auth.providers';
import { userProviders } from 'src/user/user.provider';
import { checkoutProviders } from 'src/checkout/checkout.providers';
import { productProviders } from 'src/product/product.provider';
import { HttpService } from '@nestjs/axios';
import { of } from 'rxjs';
import { KafkaModule } from '../kafka/kafka.module';
import { CacheModule } from '../cache/cache.module';
import { AiChatBotModule } from 'src/ai-chatbot/ai-chatbot.module';
import { ChatOpenAI } from '@langchain/openai';
import { Logger } from '@nestjs/common';
import { ElasticsearchConfigModule } from '../elastic-search/elastic-search.module';
import { PromptBuilderModule } from '../prompt-builder/prompt-builder.module';
import { IKafkaProducerService } from '../kafka/kafka-producer.interface';
import { PaymentGrpcService } from '../grpc/payment-grpc.service';
import { GrpcModule } from '../grpc/grpc.module';
import { DatabaseModule } from '../database/database.module';
import { LangchainChatService } from 'src/ai-chatbot/services/langchain-chat/langchain-chat.service';
import { OpenAiChatbotService } from 'src/ai-chatbot/services/open-ai/open-ai-chatbot.service';
import { LocalAiChatbotService } from 'src/ai-chatbot/services/local-ai/local-ai-chatbot.service';
import { IPromptBuilderService } from '../prompt-builder/prompt-builder.interface';
import OpenAI from 'openai';

export const createTestingModule = async (
  options: {
    override?: (builder: TestingModuleBuilder) => TestingModuleBuilder;
  } = {},
) => {
  let builder = Test.createTestingModule({
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
      // AiChatBotModule, // Commented out to avoid conflicts with our mocks
      ElasticsearchConfigModule,
      // PromptBuilderModule, // Commented out to avoid conflicts with our mocks
      GrpcModule,
    ],
    exports: [],
    providers: [
      ...authProviders,
      ...userProviders,
      ...checkoutProviders,
      ...productProviders,
      {
        provide: IKafkaProducerService,
        useValue: {
          sendMessage: jest.fn(),
        },
      },
      {
        provide: ChatOpenAI,
        useValue: {
          invoke: jest.fn().mockResolvedValue({
            content:
              'Mocked AI response for testing purposes. This is a comprehensive analysis with Price Range: $999.00 - $1299.00, Conclusion: REASONABLE, Analysis: Based on market research and feature analysis.',
          }),
          pipe: jest.fn().mockReturnThis(),
          // Mock any other methods that might be called
          call: jest.fn().mockResolvedValue({
            content: 'Mocked call response',
          }),
          stream: jest.fn().mockReturnValue({
            forEach: jest.fn(),
            toArray: jest.fn().mockResolvedValue([]),
          }),
        },
      },
      {
        provide: LangchainChatService,
        useValue: {
          basicChat: jest.fn().mockResolvedValue('Mocked basic chat response'),
          contextAwareChat: jest
            .fn()
            .mockResolvedValue('Mocked context aware response'),
          agentChat: jest
            .fn()
            .mockResolvedValue(
              'Mocked agent chat response with Price Range: $999.00 - $1299.00, Conclusion: REASONABLE, Analysis: Based on market research and feature analysis.',
            ),
          // Mock private methods that might be called
          successResponse: jest.fn().mockReturnValue('Mocked success response'),
          loadSingleChain: jest.fn().mockReturnValue({
            invoke: jest
              .fn()
              .mockResolvedValue(new Uint8Array([72, 101, 108, 108, 111])), // "Hello"
            pipe: jest.fn().mockReturnThis(),
          }),
          createPromptTemplate: jest.fn().mockReturnValue({
            pipe: jest.fn().mockReturnThis(),
          }),
          formateMessage: jest.fn().mockReturnValue('user: Mocked message'),
          formatBaseMessages: jest.fn().mockReturnValue('Mocked base message'),
        },
      },
      {
        provide: OpenAiChatbotService,
        useValue: {
          createChatCompletion: jest.fn().mockResolvedValue({
            id: 'mock-openai-id',
            choices: [
              {
                message: {
                  role: 'assistant',
                  content: 'Mocked OpenAI response',
                },
              },
            ],
          }),
        },
      },
      {
        provide: LocalAiChatbotService,
        useValue: {
          createChatCompletion: jest.fn().mockResolvedValue({
            id: 'mock-local-ai-id',
            choices: [
              {
                message: {
                  role: 'assistant',
                  content: 'Mocked Local AI response',
                },
              },
            ],
          }),
        },
      },
      {
        provide: IPromptBuilderService,
        useValue: {
          buildPriceRange: jest
            .fn()
            .mockReturnValue('Mocked price range prompt template'),
        },
      },
      {
        provide: OpenAI,
        useValue: {
          chat: {
            completions: {
              create: jest.fn().mockResolvedValue({
                id: 'mock-openai-completion-id',
                choices: [
                  {
                    message: {
                      role: 'assistant',
                      content: 'Mocked OpenAI completion response',
                    },
                  },
                ],
              }),
            },
          },
        },
      },
      {
        provide: 'OLLAMA_API_URL',
        useValue: 'http://localhost:3000',
      },
      {
        provide: 'ElasticsearchService',
        useValue: {
          index: jest.fn().mockResolvedValue({ acknowledged: true }),
          search: jest.fn().mockResolvedValue({ hits: { hits: [] } }),
          update: jest.fn().mockResolvedValue({ acknowledged: true }),
          delete: jest.fn().mockResolvedValue({ acknowledged: true }),
        },
      },
      {
        provide: Logger,
        useValue: {
          error: jest.fn(),
          log: jest.fn(),
          warn: jest.fn(),
          debug: jest.fn(),
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
  });

  if (options.override) {
    builder = options.override(builder);
  }

  return builder.compile();
};
