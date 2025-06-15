import { Cart } from 'src/checkout/core/entity/cart/cart';
import { RemoveFromCartDto } from 'src/checkout/interface/dtos/remove-from-cart.dto';

export abstract class IRemoveFromCartUseCase {
  abstract execute(dto: RemoveFromCartDto): Promise<Cart>;
}
