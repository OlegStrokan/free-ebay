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
import { IUpdatePaymentStatusUseCase } from '../epplication/use-cases/update-payment-status/update-payment-status.interface';
import { PaymentStatus } from 'src/checkout/core/entity/payment/payment';
import { UpdatePaymentStatusDto } from './dtos/update-payment-status.dto';
import { ApiTags, ApiOperation, ApiBody, ApiParam } from '@nestjs/swagger';

import { AddToCartDto } from './dtos/add-to-cart.dto';
import { RemoveFromCartDto } from './dtos/remove-from-cart.dto';
import { CreateCartDto } from './dtos/create-cart.dto';
import { IRemoveFromCartUseCase } from '../epplication/use-cases/remove-from-cart/remove-from-cart.interface';
import { CreateOrderDto } from './dtos/create-order.dto';

@ApiTags('Checkout')
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
    private checkPaymentStatusUseCase: ICheckPaymentStatusUseCase,
    private createCartUseCase: ICreateCartUseCase,
    private updatePaymentStatusUseCase: IUpdatePaymentStatusUseCase,
  ) {}

  @Post('cart')
  @ApiOperation({ summary: 'Add item to cart' })
  @ApiBody({ type: AddToCartDto })
  addToCart(@Body() dto: AddToCartDto) {
    return this.addToCartUseCase.execute(dto);
  }

  @Patch('cart')
  @ApiOperation({ summary: 'Remove item from cart' })
  @ApiBody({ type: RemoveFromCartDto })
  removeFromCart(@Body() dto: RemoveFromCartDto) {
    return this.removeFromCartUseCase.execute(dto);
  }

  @Post('cart/create')
  @ApiOperation({ summary: 'Create a new cart' })
  @ApiBody({ type: CreateCartDto })
  createCart(@Body() dto: CreateCartDto) {
    return this.createCartUseCase.execute(dto);
  }

  @Get('cart')
  @ApiOperation({ summary: 'Get user cart' })
  @ApiParam({ name: 'userId', required: true })
  getCart(@Param('userId') userId: string) {
    return this.retrieveCartUseCase.execute(userId);
  }

  @Delete('cart')
  @ApiOperation({ summary: 'Clear user cart' })
  @ApiParam({ name: 'userId', required: true })
  clearCart(@Param('userId') userId: string) {
    return this.clearCartUseCase.execute(userId);
  }

  @Post('order')
  @ApiOperation({ summary: 'Create an order' })
  @ApiBody({ type: Object })
  createOrder(@Body() dto: CreateOrderDto) {
    return this.createOrderUseCase.execute(dto);
  }

  @Get('order/:id')
  @ApiOperation({ summary: 'Get order details' })
  @ApiParam({ name: 'id', required: true })
  getOrderDetails(@Param('id') id: string) {
    return this.getOrderDetailsUseCase.execute(id);
  }

  @Get('orders')
  @ApiOperation({ summary: 'Get all user orders' })
  @ApiParam({ name: 'id', required: true })
  getAllOrders(@Param('id') id: string) {
    return this.getAllOrdersUseCase.execute(id);
  }

  @Patch('order/:id/cancel')
  @ApiOperation({ summary: 'Cancel an order' })
  @ApiParam({ name: 'id', required: true })
  cancelOrder(@Param('id') id: string) {
    return this.cancelOrderUseCase.execute(id);
  }

  @Patch('order/:id/ship')
  @ApiOperation({ summary: 'Ship an order' })
  @ApiParam({ name: 'id', required: true })
  shipOrder(@Param('id') id: string) {
    return this.shipOrderUseCase.execute(id);
  }

  // Note: Payment processing is now handled directly in POST /order endpoint
  // This endpoint is deprecated - use POST /order instead

  @Post('payment/update-status')
  @ApiOperation({ summary: 'Update payment status' })
  @ApiBody({ type: UpdatePaymentStatusDto })
  async updatePaymentStatus(@Body() body: UpdatePaymentStatusDto) {
    const { paymentIntentId, newStatus } = body;
    await this.updatePaymentStatusUseCase.execute(
      paymentIntentId,
      newStatus as PaymentStatus,
    );
    return { success: true };
  }

  @Get('payment/status/:id')
  @ApiOperation({ summary: 'Check payment status' })
  @ApiParam({ name: 'id', required: true })
  checkPaymentStatus(@Param('id') id: string) {
    return this.checkPaymentStatusUseCase.execute(id);
  }
}
