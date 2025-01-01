// src/infrastructure/repositories/payment-status.repository.ts
import { Injectable } from '@nestjs/common';
import { Repository } from 'typeorm';
import { InjectRepository } from '@nestjs/typeorm';
import { PaymentStatus } from 'src/checkout/core/entity/payment-status';
import { IPaymentStatusRepository } from 'src/checkout/core/repository/payment-status.repository';
import { PaymentStatusDb } from '../entity/payment-status.entity';

@Injectable()
export class PaymentStatusRepository implements IPaymentStatusRepository {
  constructor(
    @InjectRepository(PaymentStatusDb)
    private readonly paymentStatusRepository: Repository<PaymentStatusDb>,
  ) {}

  async createStatus(
    statusData: Partial<PaymentStatus>,
  ): Promise<PaymentStatus> {
    const status = this.paymentStatusRepository.create(statusData);
    return this.paymentStatusRepository.save(status);
  }

  async findById(statusId: string): Promise<PaymentStatus> {
    return this.paymentStatusRepository.findOne(statusId);
  }

  async findByPaymentId(paymentId: string): Promise<PaymentStatus[]> {
    return this.paymentStatusRepository.find({ where: { paymentId } });
  }
}
