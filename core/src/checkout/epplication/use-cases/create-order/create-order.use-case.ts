import { Inject, Injectable } from '@nestjs/common';
import { ICreateOrderUseCase } from './create-order.interface';
import { ICartRepository } from 'src/checkout/core/repository/cart.repository';

import { CartNotFoundException } from 'src/checkout/core/exceptions/cart/cart-not-found.exception';
import { IOrderRepository } from 'src/checkout/core/repository/order.repository';
import { Order } from 'src/checkout/core/entity/order/order';
import { OrderItem } from 'src/checkout/core/entity/order-item/order-item';
import { CreateOrderDto } from 'src/checkout/interface/dtos/create-order.dto';
import { Cart } from 'src/checkout/core/entity/cart/cart';
import { Shipment } from 'src/checkout/core/entity/shipment/shipment';
import {
  Payment,
  PaymentMethod,
} from 'src/checkout/core/entity/payment/payment';
import { Money } from 'src/shared/types/money';
import { IShipmentRepository } from 'src/checkout/core/repository/shipment.repository';
import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { CartItemsNotFoundException } from 'src/checkout/core/exceptions/cart/cart-items-not-found.exception';
import { firstValueFrom } from 'rxjs';
import { PaymentFailedException } from 'src/checkout/core/exceptions/payment/payment-failed.exception';
import { HttpService } from '@nestjs/axios';

@Injectable()
export class CreateOrderUseCase implements ICreateOrderUseCase {
  constructor(
    private readonly cartRepository: ICartRepository,
    private readonly orderRepository: IOrderRepository,
    private readonly shipmentRepository: IShipmentRepository,
    private readonly paymentRepository: IPaymentRepository,
    private readonly httpService: HttpService,
  ) {}

  async execute(dto: CreateOrderDto): Promise<Order> {
    const cart = await this.cartRepository.getOneByIdIdWithRelations(
      dto.cartId,
    );
    if (!cart) {
      throw new CartNotFoundException('id', dto.cartId);
    }

    if (cart.items.length === 0) {
      throw new CartItemsNotFoundException(cart.id);
    }

    const order = this.createOrderFromCart(cart);

    const shipment = await this.createShipment(order.id, dto.shippingAddress);
    const payment = await this.createPayment(
      order.id,
      dto.paymentMethod,
      order.totalPrice,
    );

    if (dto.paymentMethod !== PaymentMethod.CashOnDelivery) {
      const response = await firstValueFrom(
        await this.processPaymentInfo(payment),
      );
      if (response.status !== 200) {
        throw new PaymentFailedException(order.id);
      }
    }

    order.data.shipment = shipment.data;
    order.data.payment = payment.data;

    const emptyCart = cart.clearCart();
    const savedOrder = await this.orderRepository.save(order);
    await this.cartRepository.updateCart(emptyCart);

    return savedOrder;
  }

  private createOrderFromCart(cart: Cart): Order {
    const order = Order.create({
      userId: cart.userId,
      totalPrice: cart.totalPrice,
    });

    const orderItems = cart.items.map(
      (cartItem) =>
        OrderItem.create({
          orderId: order.id,
          priceAtPurchase: cartItem.price,
          productId: cartItem.productId,
          quantity: cartItem.quantity,
        }).data,
    );

    const orderWithItems = order.addItems(orderItems);

    return orderWithItems;
  }

  private async createShipment(
    orderId: string,
    shippingAddress: string,
  ): Promise<Shipment> {
    const shipment = Shipment.create(orderId, shippingAddress);
    await this.shipmentRepository.save(shipment);
    return shipment;
  }

  private async createPayment(
    orderId: string,
    paymentMethod: PaymentMethod,
    amount: Money,
  ): Promise<Payment> {
    const payment = Payment.create({ amount, paymentMethod, orderId });
    await this.paymentRepository.save(payment);
    return payment;
  }

  private async processPaymentInfo(payment: Payment) {
    const paymentInfo = {
      orderId: payment.orderId,
      amount: {
        amount: payment.amount.getAmount(),
        fraction: payment.amount.getFraction(),
        currency: payment.amount.getCurrency(),
      },
      paymentMethod: payment.paymentMethod,
    };
    return this.httpService.post(
      'http://localhost:5012/api/Payment/ProcessPayment',
      paymentInfo,
    );
  }
}
