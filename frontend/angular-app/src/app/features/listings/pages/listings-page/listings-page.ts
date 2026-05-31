import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';

import { CatalogProduct } from '../../models/catalog-product';
import { CatalogApi } from '../../services/catalog-api';

@Component({
  selector: 'app-listings-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './listings-page.html',
  styleUrl: './listings-page.css',
})
export class ListingsPageComponent {
  private readonly catalogApi = inject(CatalogApi);

  protected readonly products$ = this.catalogApi.getProducts();

  protected getLowestPrice(product: CatalogProduct): number | null {
    if (product.variants.length === 0) {
      return null;
    }

    return Math.min(...product.variants.map((variant) => variant.priceAmountMinor));
  }

  protected formatPrice(priceAmountMinor: number, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(priceAmountMinor / 100);
  }
}