import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';

export interface PaymentData {
  id: string;
  orderId: string;
  amount: Money;
  paymentMethod: string;
  status: string;
  createdAt: Date;
  updatedAt: Date;
}

export class Payment implements Clonable<Payment> {
  constructor(public payment: PaymentData) {}

  static create = (
    paymentData: Omit<PaymentData, 'id' | 'createdAt' | 'updatedAt'>,
  ) =>
    new Payment({
      ...paymentData,
      id: generateUlid(),
      createdAt: new Date(),
      updatedAt: new Date(),
    });

  get id(): string {
    return this.payment.id;
  }

  get orderId(): string {
    return this.payment.orderId;
  }

  get amount(): Money {
    return this.payment.amount;
  }

  get paymentMethod(): string {
    return this.payment.paymentMethod;
  }

  markAsPaid = () => {
    const clone = this.clone();
    clone.payment.status = 'Paid';
    return clone;
  };

  clone = (): Payment => new Payment({ ...this.payment });
}
