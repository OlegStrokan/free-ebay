import { Cart } from 'src/checkout/core/entity/cart/cart';
import { CartDb } from '../../entity/cart.entity';
import { CartData } from 'src/checkout/core/entity/cart/cart';

export abstract class ICartMapper {
  abstract toDb(domain: Cart): CartDb;
  abstract toDomain(db: CartDb): Cart;
  abstract toClient(domain: Cart): CartData;
}
