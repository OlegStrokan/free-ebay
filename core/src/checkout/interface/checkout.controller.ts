import {
  Controller,
  Post,
  Get,
  Delete,
  Patch,
  Param,
  Body,
} from '@nestjs/common';
import { Inject } from '@nestjs/common';
import { IAddToCartUseCase } from '../epplication/use-cases/add-to-cart/add-to-cart.interface';
import { ICancelOrderUseCase } from '../epplication/use-cases/cancel-order/cancel-order.interface';
import { ICheckPaymentStatusUseCase } from '../epplication/use-cases/check-payment-status/check-payment-status.interface';
import { IClearCartUseCase } from '../epplication/use-cases/clear-cart/clear-cart.interface';
import { ICreateOrderUseCase } from '../epplication/use-cases/create-order/create-order.interface';
import { IGetAllOrdersUseCase } from '../epplication/use-cases/get-all-orders/get-all-orders.interface';
import { IGetOrderDetailsUseCase } from '../epplication/use-cases/get-order-detail/get-order-detail.interface';
import { IProceedPaymentUseCase } from '../epplication/use-cases/process-payment/process-payment.interface';
import { IRetrieveCartUseCase } from '../epplication/use-cases/retrieve-cart/retrieve-cart.interface';
import { IShipOrderUseCase } from '../epplication/use-cases/ship-order/ship-order.interface';

@Controller('checkout')
export class CheckoutController {
  constructor(
    @Inject(IAddToCartUseCase) private addToCartUseCase: IAddToCartUseCase,
    @Inject(IRetrieveCartUseCase)
    private retrieveCartUseCase: IRetrieveCartUseCase,
    @Inject(IClearCartUseCase) private clearCartUseCase: IClearCartUseCase,
    @Inject(ICreateOrderUseCase)
    private createOrderUseCase: ICreateOrderUseCase,
    @Inject(IGetOrderDetailsUseCase)
    private getOrderDetailsUseCase: IGetOrderDetailsUseCase,
    @Inject(IGetAllOrdersUseCase)
    private getAllOrdersUseCase: IGetAllOrdersUseCase,
    @Inject(ICancelOrderUseCase)
    private cancelOrderUseCase: ICancelOrderUseCase,
    @Inject(IShipOrderUseCase) private shipOrderUseCase: IShipOrderUseCase,
    @Inject(IProceedPaymentUseCase)
    private proceedPaymentUseCase: IProceedPaymentUseCase,
    @Inject(ICheckPaymentStatusUseCase)
    private checkPaymentStatusUseCase: ICheckPaymentStatusUseCase,
    @Inject(ICreateCartUseCase)
    private createCartUseCase: ICreateCartUseCase,
  ) {}

  @Post('cart')
  addToCart(@Body() dto: any) {
    return this.addToCartUseCase.execute(dto);
  }

  @Post('cart/create')
  createCart(@Body() dto: any) {
    return this.createCartUseCase.execute(dto);
  }

  @Get('cart')
  getCart() {
    return this.retrieveCartUseCase.execute(null);
  }

  @Delete('cart')
  clearCart() {
    return this.clearCartUseCase.execute(null);
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
  getAllOrders() {
    return this.getAllOrdersUseCase.execute(null);
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
