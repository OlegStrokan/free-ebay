import { Cart } from 'src/checkout/core/entity/cart/cart';
import { CartDb } from '../../entity/cart.entity';
import { CartDto } from 'src/checkout/interface/dtos/cart.dto';

export abstract class ICartMapper {
  abstract toDb(domain: Cart): CartDb;
  abstract toDomain(db: CartDb): Cart;
  abstract toClient(domain: Cart): CartDto;
}
