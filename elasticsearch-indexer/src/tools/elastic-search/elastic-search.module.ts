import { Module } from '@nestjs/common';
import { ConfigModule, ConfigService } from '@nestjs/config';
import { ElasticsearchModule } from '@nestjs/elasticsearch';

@Module({
  imports: [
    ElasticsearchModule.registerAsync({
      imports: [ConfigModule],
      useFactory: async (config: ConfigService) => ({
        node: config.get('ELASTIC_NODE', 'http://localhost:9200'),
        auth: {
          username: config.get('ELASTIC_USERNAME', 'elastic'),
          password: config.get('ELASTIC_PASSWORD', 'yourpassword'),
        },
        tls: {
          rejectUnauthorized: false,
        },
      }),
      inject: [ConfigService],
    }),
  ],
  exports: [ElasticsearchModule],
})
export class ElasticsearchConfigModule {}
