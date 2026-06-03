import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CartStore } from '../../../cart/services/cart-store';
import { CatalogProduct, CatalogProductVariant } from '../../models/catalog-product';
import { CatalogApi } from '../../services/catalog-api';

@Component({
  selector: 'app-listings-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './listings-page.html',
  styleUrl: './listings-page.css',
})
export class ListingsPageComponent {
  private readonly catalogApi = inject(CatalogApi);

  protected readonly cartStore = inject(CartStore);
  protected readonly products$ = this.catalogApi.getProducts();

  protected getLowestPrice(product: CatalogProduct): number | null {
    if (product.variants.length === 0) {
      return null;
    }

    return Math.min(...product.variants.map((variant) => variant.priceAmountMinor));
  }

  protected getFirstActiveVariant(product: CatalogProduct): CatalogProductVariant | null {
    return product.variants.find((variant) => variant.isActive) ?? product.variants[0] ?? null;
  }

  protected addFirstVariantToCart(product: CatalogProduct): void {
    const variant = this.getFirstActiveVariant(product);

    if (!variant) {
      return;
    }

    this.cartStore.addItem({
      productId: product.id,
      productVariantId: variant.id,
      sku: variant.sku,
      productName: product.name,
      variantName: variant.name,
      unitPriceAmountMinor: variant.priceAmountMinor,
      currency: variant.currency,
    });
  }

  protected formatPrice(priceAmountMinor: number, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(priceAmountMinor / 100);
  }
}