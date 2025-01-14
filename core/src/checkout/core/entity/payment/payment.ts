import { Clonable } from 'src/shared/types/clonable';
import { generateUlid } from 'src/shared/types/generate-ulid';
import { Money } from 'src/shared/types/money';

export enum PaymentMethod {
  CreditCard = 'CreditCard',
  PayPal = 'Paypal',
  BankTransfer = 'BankTransfer',
  CashOnDelivery = 'CashOnDelivery',
  ApplePay = 'ApplePay',
  GooglePay = 'GooglePay',
  Cryptocurrency = 'Cryptocurrency',
}

export enum PaymentStatus {
  Pending = 'Pending',
  Completed = 'Completed',
  Paid = 'Paid',
  Failed = 'Failed',
  Refunded = 'Refunded',
  Cancelled = 'Cancelled',
}

export interface PaymentData {
  id: string;
  orderId: string;
  amount: Money;
  paymentMethod: PaymentMethod;
  status: PaymentStatus;
  createdAt: Date;
  updatedAt: Date;
}

export class Payment implements Clonable<Payment> {
  constructor(public payment: PaymentData) {}

  static create = (
    paymentData: Omit<PaymentData, 'id' | 'createdAt' | 'updatedAt' | 'status'>,
  ) =>
    new Payment({
      ...paymentData,
      id: generateUlid(),
      status: PaymentStatus.Pending,
      createdAt: new Date(),
      updatedAt: new Date(),
    });

  get id(): string {
    return this.payment.id;
  }

  get data(): PaymentData {
    return this.payment;
  }

  get orderId(): string {
    return this.payment.orderId;
  }

  get amount(): Money {
    return this.payment.amount;
  }

  get paymentMethod(): PaymentMethod {
    return this.payment.paymentMethod;
  }

  updateStatus = (status: PaymentStatus) => {
    const clone = this.clone();
    clone.payment.status = status;
    return clone;
  };

  updatePaymentMethod = (paymentMethod: PaymentMethod) => {
    const clone = this.clone();
    clone.payment.paymentMethod = paymentMethod;
    return clone;
  };

  clone = (): Payment => new Payment({ ...this.payment });
}
