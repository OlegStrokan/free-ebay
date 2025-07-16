import { Injectable } from '@nestjs/common';
import {
  Payment,
  PaymentMethod,
  PaymentStatus,
} from 'src/checkout/core/entity/payment/payment';
import { Shipment } from 'src/checkout/core/entity/shipment/shipment';
import { PaymentFailedException } from 'src/checkout/core/exceptions/payment/payment-failed.exception';
import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { IShipmentRepository } from 'src/checkout/core/repository/shipment.repository';
import { Money } from 'src/shared/types/money';
import {
  IInitiatePaymentUseCase,
  PaymentResult,
} from './initiate-payment.interface';
import { ProceedPaymentDto } from 'src/checkout/interface/dtos/proceed-payment.dto';
import { PaymentGrpcService } from 'src/shared/grpc/payment-grpc.service';

@Injectable()
export class InitiatePaymentUseCase implements IInitiatePaymentUseCase {
  constructor(
    private readonly shipmentRepository: IShipmentRepository,
    private readonly paymentRepository: IPaymentRepository,
    private readonly paymentGrpcService: PaymentGrpcService,
  ) {}

  async execute(dto: ProceedPaymentDto): Promise<PaymentResult> {
    const shipment = await this.createShipment(
      dto.orderId,
      dto.shippingAddress,
    );

    let confirmedPayment = await this.createPayment(
      dto.orderId,
      dto.paymentMethod,
      dto.amount,
    );

    if (dto.paymentMethod !== PaymentMethod.CashOnDelivery) {
      const response = await this.processPaymentInfo(confirmedPayment);

      if (response.status < 200 || response.status >= 300 || !response.data) {
        throw new PaymentFailedException(dto.orderId);
      }

      const paymentResponse = response.data;
      confirmedPayment = confirmedPayment.updateStatus(
        paymentResponse.paymentStatus as PaymentStatus,
      );
      await this.paymentRepository.save(confirmedPayment);
    }

    return {
      shipment,
      payment: confirmedPayment,
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
    return await this.paymentGrpcService.processPayment(
      payment.id,
      payment.orderId,
      payment.amount,
      payment.paymentMethod,
    );
  }
}
