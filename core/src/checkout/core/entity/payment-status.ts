import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';

export interface PaymentStatusData {
  id: string;
  paymentStatus: string;
  transactionId: string;
  amount: Money;
  createdAt: Date;
  updatedAt: Date;
}

export class PaymentStatus implements Clonable<PaymentStatus> {
  constructor(public status: PaymentStatusData) {}

  static create = (
    paymentStatusData: Omit<
      PaymentStatusData,
      'id' | 'createdAt' | 'updatedAt'
    >,
  ) =>
    new PaymentStatus({
      ...paymentStatusData,
      id: generateUlid(),
      createdAt: new Date(),
      updatedAt: new Date(),
    });

  get id(): string {
    return this.status.id;
  }

  get paymentStatus(): string {
    return this.status.paymentStatus;
  }

  get transactionId(): string {
    return this.status.transactionId;
  }

  get amount(): Money {
    return this.status.amount;
  }

  clone = (): PaymentStatus => new PaymentStatus({ ...this.status });
}
