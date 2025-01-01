import { AddToCartDto } from 'src/checkout/interface/dtos/add-to-card.dto';
import { IUseCase } from 'src/shared/types/use-case.interface';

export type IAddToCartUseCase = IUseCase<AddToCartDto, void>;
