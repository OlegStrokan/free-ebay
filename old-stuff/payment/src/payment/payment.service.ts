import { Injectable, Logger, OnModuleDestroy } from '@nestjs/common';
import { Subject, Observable } from 'rxjs';
import {
  Money,
  PaymentMethod,
  PaymentStatus,
} from 'src/generated/protos/payment';

export interface StoredPayment {
  payment_id: string;
  order_id: string;
  amount: Money;
  payment_method: PaymentMethod;
  status: PaymentStatus;
  transaction_id?: string;
  client_secret?: string;
}

@Injectable()
export class PaymentService implements OnModuleDestroy {
  private readonly logger = new Logger(PaymentService.name);
  private payments = new Map<string, StoredPayment>();
  private updateSubjects = new Map<string, Subject<any>>();

  async processPayment(request: {
    payment_id: string;
    order_id: string;
    amount: { amount: number; currency: string };
    payment_method: PaymentMethod;
  }) {
    const { payment_id, order_id, amount, payment_method } = request;

    const payment: StoredPayment = {
      payment_id,
      order_id,
      amount,
      payment_method,
      status: PaymentStatus.PAYMENT_STATUS_PENDING,
    };

    this.payments.set(payment_id, payment);
    this.emitUpdate(
      payment_id,
      PaymentStatus.PAYMENT_STATUS_PENDING,
      'Payment processing started',
    );

    try {
      await new Promise((res) => setTimeout(res, 500));

      payment.status = PaymentStatus.PAYMENT_STATUS_SUCCEEDED;
      payment.transaction_id = `tx_${Math.random().toString(36).slice(2, 10)}`;
      payment.client_secret = `secret_${Math.random().toString(36).slice(2, 8)}`;
      this.payments.set(payment_id, payment);

      this.emitUpdate(
        payment_id,
        PaymentStatus.PAYMENT_STATUS_SUCCEEDED,
        'Payment succeeded',
      );

      return {
        payment_id,
        status: payment.status,
        transaction_id: payment.transaction_id,
        client_secret: payment.client_secret,
        error_message: '',
      };
    } catch (err: any) {
      payment.status = PaymentStatus.PAYMENT_STATUS_FAILED;
      this.payments.set(payment_id, payment);
      this.emitUpdate(
        payment_id,
        PaymentStatus.PAYMENT_STATUS_FAILED,
        err?.message || 'Error',
      );

      return {
        payment_id,
        status: PaymentStatus.PAYMENT_STATUS_FAILED,
        transaction_id: '',
        client_secret: '',
        error_message: err?.message || 'Unknown error',
      };
    }
  }

  async getPaymentStatus(request: { payment_id: string }) {
    const p = this.payments.get(request.payment_id);
    if (!p) {
      return {
        payment_id: request.payment_id,
        status: PaymentStatus.PAYMENT_STATUS_UNKNOWN,
        order_id: '',
        amount: { amount: 0, currency: 'USD' },
        payment_method: PaymentMethod.PAYMENT_METHOD_UNKNOWN,
      };
    }

    return {
      payment_id: p.payment_id,
      status: p.status,
      order_id: p.order_id,
      amount: p.amount,
      payment_method: p.payment_method,
    };
  }

  streamPaymentUpdates(request: { payment_id: string }): Observable<any> {
    const payment_id = request.payment_id;
    let subj = this.updateSubjects.get(payment_id);
    if (!subj) {
      subj = new Subject<any>();
      this.updateSubjects.set(payment_id, subj);

      const p = this.payments.get(payment_id);
      if (p) {
        subj.next({
          payment_id: p.payment_id,
          status: p.status,
          message: 'Current status',
          timestamp: Date.now(),
        });
      }
    }
    return subj.asObservable();
  }

  private emitUpdate(
    payment_id: string,
    status: PaymentStatus,
    message: string,
  ) {
    const subj = this.updateSubjects.get(payment_id);
    if (subj) {
      subj.next({
        payment_id,
        status,
        message,
        timestamp: Date.now(),
      });
    } else {
      this.logger.debug(`No active subscribers for payment ${payment_id}`);
    }
  }

  onModuleDestroy() {
    for (const s of this.updateSubjects.values()) s.complete();
  }
}
