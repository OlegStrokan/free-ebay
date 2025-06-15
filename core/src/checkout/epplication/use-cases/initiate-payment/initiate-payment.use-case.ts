import { HttpService } from '@nestjs/axios';
import { Injectable } from '@nestjs/common';
import { firstValueFrom } from 'rxjs';
import {
  Payment,
  PaymentMethod,
} from 'src/checkout/core/entity/payment/payment';
import { Shipment } from 'src/checkout/core/entity/shipment/shipment';
import { PaymentFailedException } from 'src/checkout/core/exceptions/payment/payment-failed.exception';
import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { IShipmentRepository } from 'src/checkout/core/repository/shipment.repository';
import { Money } from 'src/shared/types/money';
import {
  IInitiatePaymentUseCase,
  InitiatePaymentDto,
  PaymentResult,
} from './initiate-payment.interface';

@Injectable()
export class InitiatePaymentUseCase implements IInitiatePaymentUseCase {
  constructor(
    private readonly shipmentRepository: IShipmentRepository,
    private readonly paymentRepository: IPaymentRepository,
    private readonly httpService: HttpService,
  ) {}

  async execute(dto: InitiatePaymentDto): Promise<PaymentResult> {
    const shipment = await this.createShipment(
      dto.orderId,
      dto.shippingAddress,
    );

    const payment = await this.createPayment(
      dto.orderId,
      dto.paymentMethod,
      dto.amount,
    );

    if (dto.paymentMethod !== PaymentMethod.CashOnDelivery) {
      const response = await firstValueFrom(
        await this.processPaymentInfo(payment),
      );

      if (response.status !== 200) {
        throw new PaymentFailedException(dto.orderId);
      }
    }

    return {
      shipment,
      payment,
    };
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
