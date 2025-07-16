import { Payment, PaymentData, PaymentMethod, PaymentStatus } from './payment';
import { Money } from 'src/shared/types/money';
import { generateUlid } from 'src/shared/types/generate-ulid';

describe('Payment', () => {
  let paymentData: PaymentData;
  let payment: Payment;

  beforeEach(() => {
    paymentData = {
      id: generateUlid(),
      orderId: 'order1',
      amount: new Money(100, 'USD', 2),
      paymentMethod: PaymentMethod.Card,
      status: PaymentStatus.Pending,
      createdAt: new Date(),
      updatedAt: new Date(),
    };
    payment = new Payment(paymentData);
  });

  test('should create a payment successfully', () => {
    const newPayment = Payment.create({
      orderId: 'order2',
      amount: new Money(200, 'USD', 2),
      paymentMethod: PaymentMethod.PayPal,
    });
    expect(newPayment).toBeInstanceOf(Payment);
    expect(newPayment.orderId).toBe('order2');
    expect(newPayment.amount.getAmount()).toBe(200);
    expect(newPayment.paymentMethod).toBe(PaymentMethod.PayPal);
    expect(newPayment.data.status).toBe(PaymentStatus.Pending);
  });

  test('should update payment status successfully', () => {
    const updatedPayment = payment.updateStatus(PaymentStatus.Completed);
    expect(updatedPayment.data.status).toBe(PaymentStatus.Completed);
  });

  test('should update payment method successfully', () => {
    const updatedPayment = payment.updatePaymentMethod(PaymentMethod.PayPal);
    expect(updatedPayment.paymentMethod).toBe(PaymentMethod.PayPal);
  });

  test('should retain original payment data after status update', () => {
    const updatedPayment = payment.updateStatus(PaymentStatus.Completed);
    expect(payment.data.status).toBe(PaymentStatus.Pending);
    expect(updatedPayment.data.status).toBe(PaymentStatus.Completed);
  });

  test('should retain original payment data after method update', () => {
    const updatedPayment = payment.updatePaymentMethod(PaymentMethod.PayPal);
    expect(payment.paymentMethod).toBe(PaymentMethod.Card);
    expect(updatedPayment.paymentMethod).toBe(PaymentMethod.PayPal);
  });
});
