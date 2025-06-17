import { Injectable } from '@nestjs/common';
import { ElasticsearchService } from '@nestjs/elasticsearch';
import { Product } from 'src/product/core/product/entity/product';

@Injectable()
export class SearchProductsUseCase {
  constructor(private readonly elasticsearchService: ElasticsearchService) {}

  async execute(query: string): Promise<Product[]> {
    const { hits } = await this.elasticsearchService.search({
      index: 'products',
      query: {
        multi_match: {
          query,
          fields: ['title^3', 'description', 'category'],
          fuzziness: 'AUTO',
        },
      },
    });

    return hits.hits.map((hit) => hit._source as Product);
  }
}
