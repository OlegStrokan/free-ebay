import { Injectable } from '@nestjs/common';
import { Repository } from 'typeorm';
import { InjectRepository } from '@nestjs/typeorm';
import { PaymentDb } from '../entity/payment.entity';
import { IPaymentRepository } from 'src/checkout/core/repository/payment.repository';
import { IPaymentMapper } from '../mappers/payment/payment.mapper.inteface';
import { Payment } from 'src/checkout/core/entity/payment/payment';
import { IClearableRepository } from 'src/shared/types/clearable';

@Injectable()
export class PaymentRepository
  implements IPaymentRepository, IClearableRepository
{
  constructor(
    @InjectRepository(PaymentDb)
    private readonly paymentRepository: Repository<PaymentDb>,
    private readonly mapper: IPaymentMapper,
  ) {}

  async save(payment: Payment): Promise<Payment> {
    const dbPayment = this.mapper.toDb(payment.data);
    const createdPayment = await this.paymentRepository.save(dbPayment);
    return this.mapper.toDomain(createdPayment);
  }

  async findById(paymentId: string): Promise<Payment | null> {
    const payment = await this.paymentRepository.findOne({
      where: { id: paymentId },
      relations: ['order'],
    });

    return payment ? this.mapper.toDomain(payment) : null;
  }

  async update(payment: Payment): Promise<Payment> {
    const dbPayment = this.mapper.toDb(payment.data);

    const updatedPayment = await this.paymentRepository.save(dbPayment);

    return this.mapper.toDomain(updatedPayment);
  }

  async findPaymentsByOrderId(orderId: string): Promise<Payment[]> {
    const payment = await this.paymentRepository.find({
      where: { order: { id: orderId } },
      relations: ['order'],
    });

    return payment.map((payment) => this.mapper.toDomain(payment));
  }

  async findByPaymentIntentId(
    paymentIntentId: string,
  ): Promise<Payment | null> {
    const payment = await this.paymentRepository.findOneBy({ paymentIntentId });
    return payment ? this.mapper.toDomain(payment) : null;
  }

  async clear(): Promise<void> {
    await this.paymentRepository.query(`DELETE FROM "payments"`);
  }
}
