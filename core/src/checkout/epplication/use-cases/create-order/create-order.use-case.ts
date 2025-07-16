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
  PaymentStatus,
} from 'src/checkout/core/entity/payment/payment';
import { Money } from 'src/shared/types/money';
import { IShipmentRepository } from 'src/checkout/core/repository/shipment.repository';
import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { CartItemsNotFoundException } from 'src/checkout/core/exceptions/cart/cart-items-not-found.exception';
import { PaymentFailedException } from 'src/checkout/core/exceptions/payment/payment-failed.exception';
import { PaymentGrpcService } from 'src/shared/grpc/payment-grpc.service';

@Injectable()
export class CreateOrderUseCase implements ICreateOrderUseCase {
  constructor(
    private readonly cartRepository: ICartRepository,
    private readonly orderRepository: IOrderRepository,
    private readonly shipmentRepository: IShipmentRepository,
    private readonly paymentRepository: IPaymentRepository,
    private readonly paymentGrpcService: PaymentGrpcService,
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

    // the problem is we saving shipment before we saving
    const shipment = await this.createShipment(order.id, dto.shippingAddress);
    const payment = await this.createPayment(
      order.id,
      dto.paymentMethod,
      order.totalPrice,
    );

    if (dto.paymentMethod !== PaymentMethod.CashOnDelivery) {
      const response = await this.processPaymentInfo(payment);
      if (response.status !== 200) {
        throw new PaymentFailedException(order.id);
      }
    }

    order.data.shipment = shipment.data;
    order.data.payment = payment.data;

    const emptyCart = cart.clearCart();
    await this.shipmentRepository.save(shipment);
    await this.paymentRepository.save(payment);

    const savedOrder = await this.orderRepository.update(order);
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
    return shipment;
  }

  private async createPayment(
    orderId: string,
    paymentMethod: PaymentMethod,
    amount: Money,
  ): Promise<Payment> {
    const payment = Payment.create({ amount, paymentMethod, orderId });
    return payment;
  }

  private async processPaymentInfo(payment: Payment) {
    return await this.paymentGrpcService.processPayment(
      payment.id,
      payment.orderId,
      payment.amount,
      payment.paymentMethod,
    );
  }
}
