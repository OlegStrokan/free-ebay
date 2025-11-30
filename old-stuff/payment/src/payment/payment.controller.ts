import { Controller } from '@nestjs/common';
import { GrpcMethod } from '@nestjs/microservices';
import { PaymentService } from './payment.service';
import { Observable } from 'rxjs';

@Controller()
export class PaymentController {
  constructor(private readonly paymentService: PaymentService) {}

  @GrpcMethod('PaymentService', 'ProcessPayment')
  async processPayment(data: any) {
    // data shape matches proto ProcessPaymentRequest
    return this.paymentService.processPayment(data);
  }

  @GrpcMethod('PaymentService', 'GetPaymentStatus')
  async getPaymentStatus(data: any) {
    return this.paymentService.getPaymentStatus(data);
  }

  // server streaming: returns Observable<PaymentUpdate>
  @GrpcMethod('PaymentService', 'StreamPaymentUpdates')
  streamPaymentUpdates(data: any): Observable<any> {
    return this.paymentService.streamPaymentUpdates(data);
  }
}
