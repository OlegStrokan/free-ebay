import { Injectable } from '@nestjs/common';
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
import { DataSource } from 'typeorm';

// Saga choreography style: monolith version
@Injectable()
export class CreateOrderUseCase implements ICreateOrderUseCase {
  constructor(
    private readonly cartRepository: ICartRepository,
    private readonly paymentGrpcService: PaymentGrpcService,
    private readonly dataSource: DataSource,
  ) {}

  async execute(dto: CreateOrderDto): Promise<Order> {
    const { cart, order, payment, shipment } =
      await this.dataSource.transaction(async (manager) => {
        const cart = await this.cartRepository.getOneByIdIdWithRelations(
          dto.cartId,
        );

        if (!cart) throw new CartNotFoundException('id', dto.cartId);
        if (cart.items.length === 0)
          throw new CartItemsNotFoundException(cart.id);
        const order = this.createOrderFromCart(cart);
        await manager.save(order);
        const shipment = Shipment.create(order.id, dto.shippingAddress);

        // Save shipment first before any external calls
        await manager.save(shipment);
        const payment = Payment.create({
          orderId: order.id,
          paymentMethod: dto.paymentMethod,
          // @todo amount, price, total price. WFT? learn fucking DDD
          amount: order.totalPrice,
        });
        await manager.save(payment);

        // Link shipment and payment to order
        order.data.shipment = shipment.data;
        order.data.payment = payment.data;

        // Save payment and update order

        // Clear cart
        const emptyCart = cart.clearCart();
        await manager.save(emptyCart);

        return { order, shipment, payment, cart };
      });

    // Process payment via gRPC if not cash on delivery
    if (dto.paymentMethod !== PaymentMethod.CashOnDelivery) {
      const response = await this.paymentGrpcService.processPayment(
        payment.id,
        payment.orderId,
        payment.amount,
        payment.paymentMethod,
      );

      await this.dataSource.transaction(async (manager) => {
        if (response.status === 200) {
          payment.updateStatus(PaymentStatus.Failed);
        } else {
          // @todo add compensation logic here
          throw new PaymentFailedException(order.id);
        }

        await manager.save(payment);
        order.data.payment = payment.data;
        await manager.save(order);
      });
    }

    return order;
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
}
