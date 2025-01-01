// src/infrastructure/repositories/payment.repository.ts
import { Injectable } from '@nestjs/common';
import { Repository } from 'typeorm';
import { InjectRepository } from '@nestjs/typeorm';
import { PaymentDb } from '../entity/payment.entity';
import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { Payment } from 'src/checkout/core/entity/payment';

@Injectable()
export class PaymentRepository implements IPaymentRepository {
  constructor(
    @InjectRepository(PaymentDb)
    private readonly paymentRepository: Repository<PaymentDb>,
  ) {}

  async createPayment(paymentData: Partial<Payment>): Promise<Payment> {
    const payment = this.paymentRepository.create(paymentData);
    return this.paymentRepository.save(payment);
  }

  async findById(paymentId: string): Promise<Payment> {
    return this.paymentRepository.findOne(paymentId);
  }

  async updatePaymentStatus(
    paymentId: string,
    status: string,
  ): Promise<Payment> {
    const payment = await this.findById(paymentId);
    payment.status = status;
    return this.paymentRepository.save(payment);
  }

  async findPaymentsByOrderId(orderId: string): Promise<Payment[]> {
    return this.paymentRepository.find({ where: { orderId } });
  }
}
