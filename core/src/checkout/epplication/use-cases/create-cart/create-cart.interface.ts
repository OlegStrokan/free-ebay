import { Cart } from 'src/checkout/core/entity/cart/cart';
import { CreateCartDto } from 'src/checkout/interface/dtos/create-cart.dto';

export abstract class ICreateCartUseCase {
  abstract execute(dto: CreateCartDto): Promise<Cart>;
}
