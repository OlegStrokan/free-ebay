import { Inject, Injectable, OnModuleInit } from '@nestjs/common';
import { ClientGrpc } from '@nestjs/microservices';
import { firstValueFrom, Observable } from 'rxjs';
import { Money } from 'src/shared/types/money';
import { PaymentMethod } from 'src/checkout/core/entity/payment/payment';

// TypeScript interfaces matching proto definitions
export interface ProcessPaymentResponse {
  payment_id: string;
  status: string;
  transaction_id: string;
  client_secret: string;
  error_message: string;
}

export interface GetPaymentStatusResponse {
  payment_id: string;
  status: string;
  order_id: string;
  amount: { amount: number; currency: string; fraction: number };
  payment_method: string;
}

export interface PaymentUpdate {
  payment_id: string;
  status: string;
  message: string;
  timestamp: number;
}

interface PaymentService {
  processPayment(data: {
    id: string;
    order_id: string;
    amount: { amount: number; currency: string; fraction: number };
    payment_method: string;
  }): Observable<ProcessPaymentResponse>;
  getPaymentStatus(data: {
    payment_id: string;
  }): Observable<GetPaymentStatusResponse>;
  streamPaymentUpdates(data: { payment_id: string }): Observable<PaymentUpdate>;
}

@Injectable()
export class PaymentGrpcService implements OnModuleInit {
  private paymentService: PaymentService;

  constructor(@Inject('PAYMENT_PACKAGE') private client: ClientGrpc) {}

  onModuleInit() {
    this.paymentService =
      this.client.getService<PaymentService>('PaymentService');
  }

  async processPayment(
    id: string,
    orderId: string,
    amount: Money,
    paymentMethod: PaymentMethod,
  ) {
    try {
      const request = {
        id,
        order_id: orderId,
        amount: {
          amount: amount.getAmount(),
          currency: amount.getCurrency(),
          fraction: amount.getFraction(),
        },
        payment_method: paymentMethod,
      };

      const response = await firstValueFrom(
        this.paymentService.processPayment(request),
      );

      return {
        status: 200,
        data: {
          paymentStatus: response.status,
          transactionId: response.transaction_id,
          clientSecret: response.client_secret,
        },
      };
    } catch (error: any) {
      return {
        status: 400,
        data: {
          paymentStatus: 'Failed',
          error: error.message,
        },
      };
    }
  }

  async getPaymentStatus(paymentId: string) {
    try {
      const response = await firstValueFrom(
        this.paymentService.getPaymentStatus({ payment_id: paymentId }),
      );
      return response;
    } catch (error: any) {
      throw new Error(`Failed to get payment status: ${error.message}`);
    }
  }

  streamPaymentUpdates(paymentId: string) {
    return this.paymentService.streamPaymentUpdates({ payment_id: paymentId });
  }
}
