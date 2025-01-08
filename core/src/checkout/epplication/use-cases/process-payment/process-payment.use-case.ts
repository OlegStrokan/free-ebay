import { Inject, Injectable } from '@nestjs/common';
import { IProceedPaymentUseCase } from './process-payment.interface';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';
import { CART_REPOSITORY } from '../../injection-tokens/repository.token';

@Injectable()
export class ProceedPaymentUseCase implements IProceedPaymentUseCase {
  constructor(
    @Inject(CART_REPOSITORY)
    private readonly cartRepository: ICartRepository,
  ) {}

  async execute(dto: any): Promise<void> {
    throw new Error();
  }
}
