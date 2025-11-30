import { Product } from 'src/product/core/product/entity/product';
import { ProductDb } from '../../entity/product.entity';
import { ProductDto } from 'src/product/interface/dtos/product.dto';

export abstract class IProductMapper {
  abstract toDb(domain: Product): ProductDb;
  abstract toDomain(db: ProductDb): Product;
  abstract toClient(domain: Product): ProductDto;
}
