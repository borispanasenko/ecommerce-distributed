import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CartStore } from '../../services/cart-store';

@Component({
  selector: 'app-cart-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './cart-page.html',
  styleUrl: './cart-page.css',
})
export class CartPageComponent {
  protected readonly cartStore = inject(CartStore);

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
}
