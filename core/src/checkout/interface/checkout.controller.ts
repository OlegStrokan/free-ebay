import {
  Controller,
  Post,
  Get,
  Delete,
  Patch,
  Param,
  Body,
} from '@nestjs/common';
import { IAddToCartUseCase } from '../epplication/use-cases/add-to-cart/add-to-cart.interface';
import { ICancelOrderUseCase } from '../epplication/use-cases/cancel-order/cancel-order.interface';
import { ICheckPaymentStatusUseCase } from '../epplication/use-cases/check-payment-status/check-payment-status.interface';
import { IClearCartUseCase } from '../epplication/use-cases/clear-cart/clear-cart.interface';
import { ICreateOrderUseCase } from '../epplication/use-cases/create-order/create-order.interface';
import { IGetAllUserOrdersUseCase } from '../epplication/use-cases/get-all-user-orders/get-all-user-orders.interface';
import { IGetOrderDetailsUseCase } from '../epplication/use-cases/get-order-detail/get-order-detail.interface';
import { IRetrieveCartUseCase } from '../epplication/use-cases/retrieve-cart/retrieve-cart.interface';
import { IShipOrderUseCase } from '../epplication/use-cases/ship-order/ship-order.interface';
import { ICreateCartUseCase } from '../epplication/use-cases/create-cart/create-cart.interface';

import { AddToCartDto } from './dtos/add-to-cart.dto';
import { RemoveFromCartDto } from './dtos/remove-from-cart.dto';
import { CreateCartDto } from './dtos/create-cart.dto';
import { IRemoveFromCartUseCase } from '../epplication/use-cases/remove-from-cart/remove-from-cart.interface';
import { IInitiatePaymentUseCase } from '../epplication/use-cases/initiate-payment/initiate-payment.interface';

@Controller('checkout')
export class CheckoutController {
  constructor(
    private addToCartUseCase: IAddToCartUseCase,
    private removeFromCartUseCase: IRemoveFromCartUseCase,
    private retrieveCartUseCase: IRetrieveCartUseCase,
    private clearCartUseCase: IClearCartUseCase,
    private createOrderUseCase: ICreateOrderUseCase,
    private getOrderDetailsUseCase: IGetOrderDetailsUseCase,
    private getAllOrdersUseCase: IGetAllUserOrdersUseCase,
    private cancelOrderUseCase: ICancelOrderUseCase,
    private shipOrderUseCase: IShipOrderUseCase,
    //@fix (initiate => proceed)
    private proceedPaymentUseCase: IInitiatePaymentUseCase,
    private checkPaymentStatusUseCase: ICheckPaymentStatusUseCase,
    private createCartUseCase: ICreateCartUseCase,
  ) {}

  @Post('cart')
  addToCart(@Body() dto: AddToCartDto) {
    return this.addToCartUseCase.execute(dto);
  }

  @Patch('cart')
  removeFromCart(@Body() dto: RemoveFromCartDto) {
    return this.removeFromCartUseCase.execute(dto);
  }

  @Post('cart/create')
  createCart(@Body() dto: CreateCartDto) {
    return this.createCartUseCase.execute(dto);
  }

  @Get('cart')
  getCart(@Param('userId') userId: string) {
    return this.retrieveCartUseCase.execute(userId);
  }

  @Delete('cart')
  clearCart(@Param('userId') userId: string) {
    return this.clearCartUseCase.execute(userId);
  }

  @Post('order')
  createOrder(@Body() dto: any) {
    return this.createOrderUseCase.execute(dto);
  }

  @Get('order/:id')
  getOrderDetails(@Param('id') id: string) {
    return this.getOrderDetailsUseCase.execute(id);
  }

  @Get('orders')
  getAllOrders(@Param('id') id: string) {
    return this.getAllOrdersUseCase.execute(id);
  }

  @Patch('order/:id/cancel')
  cancelOrder(@Param('id') id: string) {
    return this.cancelOrderUseCase.execute(id);
  }

  @Patch('order/:id/ship')
  shipOrder(@Param('id') id: string) {
    return this.shipOrderUseCase.execute(id);
  }

  @Post('payment')
  proceedPayment(@Body() dto: any) {
    return this.proceedPaymentUseCase.execute(dto);
  }

  @Get('payment/status/:id')
  checkPaymentStatus(@Param('id') id: string) {
    return this.checkPaymentStatusUseCase.execute(id);
  }
}
