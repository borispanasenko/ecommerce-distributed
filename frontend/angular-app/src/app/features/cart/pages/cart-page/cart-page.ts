import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { OrderingApi } from '../../../orders/services/ordering-api';
import { CartStore } from '../../services/cart-store';

import { CHECKOUT_STOCK_LOCATION } from '../../../../core/services/checkout-config';
import { getHttpErrorMessage } from '../../../../shared/utils/http-error-message';

@Component({
  selector: 'app-cart-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './cart-page.html',
  styleUrl: './cart-page.css',
})
export class CartPageComponent {
  protected readonly cartStore = inject(CartStore);

  private readonly orderingApi = inject(OrderingApi);
  private readonly router = inject(Router);

  protected customerName = '';
  protected customerEmail = '';

  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected formatPrice(priceAmountMinor: number, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(priceAmountMinor / 100);
  }

  protected getLineTotalAmountMinor(unitPriceAmountMinor: number, quantity: number): number {
    return unitPriceAmountMinor * quantity;
  }

  protected getCartTotalAmountMinor(): number {
    return this.cartStore.items().reduce(
      (total, item) => total + item.unitPriceAmountMinor * item.quantity,
      0,
    );
  }

  protected getCartCurrency(): string {
    return this.cartStore.items()[0]?.currency ?? 'USD';
  }

  protected async checkout(): Promise<void> {
    if (this.isSubmitting()) {
    return;
  }

    this.errorMessage.set(null);

    const customerName = this.customerName.trim();
    const customerEmail = this.customerEmail.trim();

    if (this.cartStore.items().length === 0) {
      this.errorMessage.set('Cart is empty.');
      return;
    }

    if (!customerName || !customerEmail) {
      this.errorMessage.set('Customer name and email are required.');
      return;
    }

    const currencies = new Set(this.cartStore.items().map((item) => item.currency));

    if (currencies.size > 1) {
      this.errorMessage.set('Cart items must use the same currency.');
      return;
    }

    this.isSubmitting.set(true);

    try {
      const order = await firstValueFrom(
        this.orderingApi.createOrder({
          customerName,
          customerEmail,
          items: this.cartStore.items().map((item) => ({
            productId: item.productId,
            productVariantId: item.productVariantId,
            sku: item.sku,
            productName: item.productName,
            variantName: item.variantName,
            unitPriceAmountMinor: item.unitPriceAmountMinor,
            currency: item.currency,
            quantity: item.quantity,
            warehouseId: CHECKOUT_STOCK_LOCATION.warehouseId,
            locationId: CHECKOUT_STOCK_LOCATION.locationId,
          })),
        }),
      );

      this.cartStore.clear();

      await this.router.navigate(['/orders', order.id]);
    } catch (error) {
      console.error('Checkout failed', error);
      this.errorMessage.set(getHttpErrorMessage(
        error, 'Checkout failed. Check Inventory stock and Ordering API.'),
      );
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
