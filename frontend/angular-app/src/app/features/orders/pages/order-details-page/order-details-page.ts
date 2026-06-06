import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BehaviorSubject, combineLatest, firstValueFrom, map, switchMap } from 'rxjs';

import { getHttpErrorMessage } from '../../../../shared/utils/http-error-message';

import { PaymentApi } from '../../../payments/services/payment-api';
import { OrderDetails } from '../../models/order';
import { OrderingApi } from '../../services/ordering-api';

@Component({
  selector: 'app-order-details-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './order-details-page.html',
  styleUrl: './order-details-page.css',
})
export class OrderDetailsPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly orderingApi = inject(OrderingApi);
  private readonly paymentApi = inject(PaymentApi);

  private readonly refreshOrder = new BehaviorSubject<void>(undefined);

  protected readonly isPaying = signal(false);
  protected readonly paymentErrorMessage = signal<string | null>(null);

  protected readonly isCancelling = signal(false);
  protected readonly cancelErrorMessage = signal<string | null>(null);

  protected readonly order$ = combineLatest([
    this.route.paramMap.pipe(
      map((params) => {
        const orderId = params.get('id');

        if (!orderId) {
          throw new Error('Order id route parameter is required.');
        }

        return orderId;
      }),
    ),
    this.refreshOrder,
  ]).pipe(
    switchMap(([orderId]) => this.orderingApi.getOrderById(orderId)),
  );

  protected async payNow(order: OrderDetails): Promise<void> {
    if (this.isPaying()) {
      return;
    }

    if (order.status !== 'PendingPayment') {
      return;
    }

    this.paymentErrorMessage.set(null);
    this.cancelErrorMessage.set(null);
    this.isPaying.set(true);

    this.paymentErrorMessage.set(null);
    this.isPaying.set(true);

    try {
      const payment = await firstValueFrom(
        this.paymentApi.createPayment({
          orderId: order.id,
          amountMinor: order.totalAmountMinor,
          currency: order.currency,
          provider: 'Manual',
        }),
      );

      await firstValueFrom(
        this.paymentApi.succeedPayment(payment.id, {
          providerReference: `FRONTEND-APPROVED-${Date.now()}`,
        }),
      );

      this.refreshOrder.next();
    } catch (error) {
      console.error('Payment failed', error);
      this.paymentErrorMessage.set(
        getHttpErrorMessage(error, 'Payment failed. Check Payment, Ordering and Inventory APIs.'),
      );
    } finally {
      this.isPaying.set(false);
    }
  }

  protected async cancelOrder(order: OrderDetails): Promise<void> {
    if (this.isCancelling()) {
      return;
    }

    if (order.status !== 'PendingPayment') {
      return;
    }

    this.paymentErrorMessage.set(null);
    this.cancelErrorMessage.set(null);
    this.isCancelling.set(true);

    try {
      await firstValueFrom(this.orderingApi.cancelOrder(order.id));
      this.refreshOrder.next();
    } catch (error) {
      console.error('Cancel order failed', error);
      this.cancelErrorMessage.set(
        getHttpErrorMessage(error, 'Cancel order failed. Check Ordering and Inventory APIs.'),
      );
    } finally {
      this.isCancelling.set(false);
    }
  }

  protected getStatusLabel(status: string): string {
    switch (status) {
      case 'PendingPayment':
        return 'Awaiting payment';
      case 'Paid':
        return 'Paid';
      case 'Cancelled':
        return 'Cancelled';
      default:
        return status;
    }
  }

  protected formatPrice(priceAmountMinor: number, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(priceAmountMinor / 100);
  }

  protected formatDate(value: string): string {
    return new Intl.DateTimeFormat('en-US', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  protected hasInventoryReservations(order: OrderDetails): boolean {
    return order.items.some((item) => item.inventoryReservationId !== null);
  }
}
