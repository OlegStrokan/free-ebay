import { ConfigModule, ConfigService } from '@nestjs/config';
import { TypeOrmModule } from '@nestjs/typeorm';
import { DbConfig } from './shared/database/database.config';
import { Module } from '@nestjs/common';
import { ProductModule } from './product/product.module';
import { ProductDb } from './product/infrastructure/entity/product.entity';
import { UserDb } from './user/infrastructure/entity/user.entity';
import { AuthModule } from './auth/auth.module';
import { UserModule } from './user/user.module';
import { CatalogModule } from './catalog/catalog.module';
import { CategoryDb } from './catalog/infrastructure/entity/category';

@Module({
  imports: [
    ConfigModule.forRoot({
      isGlobal: true,
      cache: true,
      load: [DbConfig],
      envFilePath: `.env`,
    }),
    TypeOrmModule.forRootAsync({
      imports: [ConfigModule],
      useFactory: (configService: ConfigService) => ({
        ...configService.get('exchange'),
      }),
      inject: [ConfigService],
    }),
    TypeOrmModule.forFeature([UserDb, ProductDb, CategoryDb]),
    ProductModule,
    AuthModule,
    UserModule,
    CatalogModule,
  ],
  exports: [],
  providers: [],
})
export class AppModule {}
