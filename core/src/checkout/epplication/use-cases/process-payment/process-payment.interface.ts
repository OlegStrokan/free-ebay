import { Payment } from 'src/checkout/core/entity/payment';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IProceedPaymentUseCase = IUseCase<Payment, void>;