import { ConfigModule, ConfigService } from '@nestjs/config';
import { Test } from '@nestjs/testing';
import { TypeOrmModule } from '@nestjs/typeorm';
import { ProductDb } from 'src/product/infrastructure/entity/product.entity';
import { ProductModule } from 'src/product/product.module';
import { UserModule } from 'src/user/user.module';
import { AuthModule } from 'src/auth/auth.module';
import { UserDb } from 'src/user/infrastructure/entity/user.entity';
import { CatalogModule } from 'src/catalog/catalog.module';
import { CategoryDb } from 'src/catalog/infrastructure/entity/category';
import { TestDbConfig } from './database/database.config';

export const createTestingModule = async () => {
  return await Test.createTestingModule({
    imports: [
      ConfigModule.forRoot({
        isGlobal: true,
        cache: true,
        load: [TestDbConfig],
        envFilePath: `.env`,
      }),
      TypeOrmModule.forRootAsync({
        imports: [ConfigModule],
        useFactory: (configService: ConfigService) => ({
          ...configService.get('test_exchange'),
        }),
        inject: [ConfigService],
      }),
      TypeOrmModule.forFeature([ProductDb, UserDb, CategoryDb]),
      ProductModule,
      AuthModule,
      UserModule,
      CatalogModule,
    ],
    exports: [],
    providers: [],
  }).compile();
};
